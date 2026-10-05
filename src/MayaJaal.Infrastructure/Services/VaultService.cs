using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MayaJaal.Shared.Contracts;
using MayaJaal.Shared.Models;

namespace MayaJaal.Infrastructure.Services;

public sealed class VaultService : IVaultService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IEncryptionProvider _encryption;
    private readonly IKeyManager _keyManager;
    private readonly ILogger<VaultService> _logger;
    private readonly string _vaultsRoot;
    private readonly object _sync = new();
    private readonly Dictionary<string, byte[]> _unlockedKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Vault> _cache = new(StringComparer.OrdinalIgnoreCase);

    public VaultService(
        IEncryptionProvider encryption,
        IKeyManager keyManager,
        ILogger<VaultService> logger,
        string? vaultsRoot = null)
    {
        _encryption = encryption;
        _keyManager = keyManager;
        _logger = logger;
        _vaultsRoot = vaultsRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "MayaJaal",
            "Vaults");
        Directory.CreateDirectory(_vaultsRoot);
    }

    public async Task<Vault> CreateVaultAsync(string name, string password, string ownerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = _encryption.GenerateSalt(16);
        var wrappingKey = _keyManager.DeriveKey(password, salt);
        var masterKey = _keyManager.GenerateMasterKey(32);
        var wrapped = _keyManager.WrapKey(masterKey, wrappingKey);

        var vault = new Vault
        {
            Name = name.Trim(),
            OwnerId = ownerId,
            State = VaultState.LOCKED,
            Path = string.Empty,
            Encryption = new EncryptionParameters
            {
                Salt = Convert.ToBase64String(salt),
                WrappedKey = Convert.ToBase64String(wrapped),
                Algorithm = "AES-256-GCM",
                KdfAlgorithm = "Argon2id"
            }
        };

        vault.Path = Path.Combine(_vaultsRoot, vault.Id);
        Directory.CreateDirectory(vault.Path);
        Directory.CreateDirectory(GetItemsDir(vault));

        await SaveVaultAsync(vault, cancellationToken).ConfigureAwait(false);
        CryptographicOperations.ZeroMemory(wrappingKey);
        CryptographicOperations.ZeroMemory(masterKey);

        lock (_sync) _cache[vault.Id] = CloneVault(vault);
        _logger.LogInformation("Created vault {VaultId} ({Name})", vault.Id, vault.Name);
        return CloneVault(vault);
    }

    public async Task<bool> UnlockVaultAsync(string vaultId, string password, CancellationToken cancellationToken = default)
    {
        var vault = await LoadVaultAsync(vaultId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Vault '{vaultId}' not found.");

        if (string.IsNullOrEmpty(vault.Encryption.Salt) || string.IsNullOrEmpty(vault.Encryption.WrappedKey))
            throw new InvalidOperationException("Vault encryption metadata is incomplete.");

        var salt = Convert.FromBase64String(vault.Encryption.Salt);
        var wrapped = Convert.FromBase64String(vault.Encryption.WrappedKey);
        byte[] wrappingKey;
        byte[] masterKey;
        try
        {
            wrappingKey = _keyManager.DeriveKey(
                password,
                salt,
                vault.Encryption.KdfMemory,
                vault.Encryption.KdfIterations,
                vault.Encryption.KdfParallelism);
            masterKey = _keyManager.UnwrapKey(wrapped, wrappingKey);
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(ex, "Failed to unlock vault {VaultId}", vaultId);
            vault.Recovery.AttemptsRemaining = Math.Max(0, vault.Recovery.AttemptsRemaining - 1);
            vault.Recovery.LastAttemptAt = DateTime.UtcNow;
            await SaveVaultAsync(vault, cancellationToken).ConfigureAwait(false);
            return false;
        }

        lock (_sync)
        {
            if (_unlockedKeys.TryGetValue(vaultId, out var old))
            {
                CryptographicOperations.ZeroMemory(old);
            }
            _unlockedKeys[vaultId] = masterKey;
            vault.State = VaultState.UNLOCKED;
            vault.UnlockedAt = DateTime.UtcNow;
            _cache[vaultId] = CloneVault(vault);
        }

        CryptographicOperations.ZeroMemory(wrappingKey);
        await SaveVaultAsync(vault, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Unlocked vault {VaultId}", vaultId);
        return true;
    }

    public async Task LockVaultAsync(string vaultId, CancellationToken cancellationToken = default)
    {
        var vault = await LoadVaultAsync(vaultId, cancellationToken).ConfigureAwait(false);
        if (vault is null) return;

        lock (_sync)
        {
            if (_unlockedKeys.Remove(vaultId, out var key))
                CryptographicOperations.ZeroMemory(key);
            vault.State = VaultState.LOCKED;
            vault.LockedAt = DateTime.UtcNow;
            _cache[vaultId] = CloneVault(vault);
        }

        await SaveVaultAsync(vault, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Locked vault {VaultId}", vaultId);
    }

    public async Task LockAllAsync(CancellationToken cancellationToken = default)
    {
        // Lock currently unlocked vaults in memory.
        List<string> unlocked;
        lock (_sync) unlocked = _unlockedKeys.Keys.ToList();
        foreach (var id in unlocked)
            await LockVaultAsync(id, cancellationToken).ConfigureAwait(false);

        // Also mark any on-disk vaults as LOCKED (idempotent) so LockAll is meaningful after restart.
        var vaults = await ListVaultsAsync(cancellationToken).ConfigureAwait(false);
        foreach (var vault in vaults)
        {
            if (unlocked.Contains(vault.Id, StringComparer.OrdinalIgnoreCase))
                continue;
            if (vault.State == VaultState.LOCKED)
                continue;
            await LockVaultAsync(vault.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<VaultItem> AddItemAsync(string vaultId, string sourcePath, CancellationToken cancellationToken = default)
    {
        if (!IsUnlocked(vaultId))
            throw new InvalidOperationException("Vault must be unlocked before adding items.");

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Source file not found.", sourcePath);

        var vault = await LoadVaultAsync(vaultId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Vault '{vaultId}' not found.");

        byte[] key;
        lock (_sync)
        {
            if (!_unlockedKeys.TryGetValue(vaultId, out key!))
                throw new InvalidOperationException("Vault unlock key is not available.");
        }

        var itemId = Guid.NewGuid().ToString();
        var encryptedPath = Path.Combine(GetItemsDir(vault), $"{itemId}.enc");
        _encryption.EncryptFile(sourcePath, encryptedPath, key, out var iv, out var tag);

        var info = new FileInfo(sourcePath);
        var item = new VaultItem
        {
            Id = itemId,
            Name = info.Name,
            OriginalPath = sourcePath,
            EncryptedPath = encryptedPath,
            Size = info.Length,
            Hash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(sourcePath, cancellationToken).ConfigureAwait(false))).ToLowerInvariant(),
            Metadata =
            {
                ["iv"] = Convert.ToBase64String(iv),
                ["tag"] = Convert.ToBase64String(tag),
                ["algorithm"] = "AES-256-GCM"
            }
        };

        vault.Items.Add(item);
        vault.ItemCount = vault.Items.Count;
        vault.TotalSize = vault.Items.Sum(i => i.Size);
        await SaveVaultAsync(vault, cancellationToken).ConfigureAwait(false);

        lock (_sync) _cache[vaultId] = CloneVault(vault);
        return item;
    }

    public async Task RemoveItemAsync(string vaultId, string itemId, CancellationToken cancellationToken = default)
    {
        var vault = await LoadVaultAsync(vaultId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Vault '{vaultId}' not found.");

        var item = vault.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException($"Item '{itemId}' not found.");

        if (!string.IsNullOrEmpty(item.EncryptedPath) && File.Exists(item.EncryptedPath))
            File.Delete(item.EncryptedPath);

        vault.Items.Remove(item);
        vault.ItemCount = vault.Items.Count;
        vault.TotalSize = vault.Items.Sum(i => i.Size);
        await SaveVaultAsync(vault, cancellationToken).ConfigureAwait(false);
        lock (_sync) _cache[vaultId] = CloneVault(vault);
    }

    public async Task<byte[]> DecryptItemAsync(string vaultId, string itemId, CancellationToken cancellationToken = default)
    {
        if (!IsUnlocked(vaultId))
            throw new InvalidOperationException("Vault must be unlocked before decrypting items.");

        var vault = await LoadVaultAsync(vaultId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Vault '{vaultId}' not found.");

        var item = vault.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException($"Item '{itemId}' not found.");

        if (string.IsNullOrEmpty(item.EncryptedPath) || !File.Exists(item.EncryptedPath))
            throw new FileNotFoundException("Encrypted item file missing.", item.EncryptedPath);

        if (!item.Metadata.TryGetValue("iv", out var ivObj) || !item.Metadata.TryGetValue("tag", out var tagObj))
            throw new InvalidOperationException("Item metadata is missing IV/tag.");

        var iv = Convert.FromBase64String(MetaAsString(ivObj));
        var tag = Convert.FromBase64String(MetaAsString(tagObj));

        byte[] key;
        lock (_sync)
        {
            if (!_unlockedKeys.TryGetValue(vaultId, out key!))
                throw new InvalidOperationException("Vault unlock key is not available.");
        }

        var ciphertext = await File.ReadAllBytesAsync(item.EncryptedPath, cancellationToken).ConfigureAwait(false);
        item.AccessedAt = DateTime.UtcNow;
        await SaveVaultAsync(vault, cancellationToken).ConfigureAwait(false);
        return _encryption.Decrypt(ciphertext, key, iv, tag);
    }

    public async Task<Vault?> GetVaultAsync(string vaultId, CancellationToken cancellationToken = default)
    {
        var vault = await LoadVaultAsync(vaultId, cancellationToken).ConfigureAwait(false);
        return vault is null ? null : CloneVault(vault);
    }

    public async Task<IReadOnlyList<Vault>> ListVaultsAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<Vault>();
        if (!Directory.Exists(_vaultsRoot)) return result;

        foreach (var dir in Directory.EnumerateDirectories(_vaultsRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = Path.GetFileName(dir);
            var vault = await LoadVaultAsync(id, cancellationToken).ConfigureAwait(false);
            if (vault is not null)
                result.Add(CloneVault(vault));
        }

        return result;
    }

    public bool IsUnlocked(string vaultId)
    {
        lock (_sync) return _unlockedKeys.ContainsKey(vaultId);
    }

    private static string GetItemsDir(Vault vault) => Path.Combine(vault.Path, "items");

    private string GetManifestPath(string vaultId) => Path.Combine(_vaultsRoot, vaultId, "vault.json");

    private async Task SaveVaultAsync(Vault vault, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(vault.Path);
        var json = JsonSerializer.Serialize(vault, JsonOptions);
        var path = GetManifestPath(vault.Id);
        var temp = path + ".tmp";
        await File.WriteAllTextAsync(temp, json, cancellationToken).ConfigureAwait(false);
        File.Copy(temp, path, overwrite: true);
        File.Delete(temp);
    }

    private async Task<Vault?> LoadVaultAsync(string vaultId, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (_cache.TryGetValue(vaultId, out var cached))
            {
                // Keep live state for lock status
                var copy = CloneVault(cached);
                copy.State = _unlockedKeys.ContainsKey(vaultId) ? VaultState.UNLOCKED : VaultState.LOCKED;
                return copy;
            }
        }

        var path = GetManifestPath(vaultId);
        if (!File.Exists(path)) return null;

        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        var vault = JsonSerializer.Deserialize<Vault>(json, JsonOptions);
        if (vault is null) return null;

        lock (_sync)
        {
            vault.State = _unlockedKeys.ContainsKey(vaultId) ? VaultState.UNLOCKED : VaultState.LOCKED;
            _cache[vaultId] = CloneVault(vault);
        }

        return vault;
    }

    private static string MetaAsString(object? value) => value switch
    {
        null => throw new InvalidOperationException("Missing metadata value."),
        string s => s,
        System.Text.Json.JsonElement je when je.ValueKind == System.Text.Json.JsonValueKind.String => je.GetString() ?? string.Empty,
        System.Text.Json.JsonElement je => je.ToString(),
        _ => Convert.ToString(value) ?? string.Empty
    };

    private static Vault CloneVault(Vault source)
        => JsonSerializer.Deserialize<Vault>(JsonSerializer.Serialize(source, JsonOptions), JsonOptions)!;
}