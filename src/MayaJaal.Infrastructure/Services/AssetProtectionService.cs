using System.Text.Json;
using Microsoft.Extensions.Logging;
using MayaJaal.Shared.Contracts;
using MayaJaal.Shared.Models;

namespace MayaJaal.Infrastructure.Services;

public sealed class AssetProtectionService : IAssetProtectionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<AssetProtectionService> _logger;
    private readonly string _storePath;
    private readonly object _sync = new();
    private readonly Dictionary<string, ProtectedAsset> _assets = new(StringComparer.OrdinalIgnoreCase);

    public AssetProtectionService(ILogger<AssetProtectionService> logger, string? storePath = null)
    {
        _logger = logger;
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "MayaJaal",
            "Config");
        Directory.CreateDirectory(root);
        _storePath = storePath ?? Path.Combine(root, "protected-assets.json");
        Load();
    }

    public Task<ProtectedAsset> RegisterAsync(
        string path,
        AssetType type,
        string ownerId,
        int sensitivity = 5,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var full = Path.GetFullPath(path);
        lock (_sync)
        {
            var existing = _assets.Values.FirstOrDefault(a =>
                string.Equals(a.Path, full, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
                return Task.FromResult(Clone(existing));

            var asset = new ProtectedAsset
            {
                Path = full,
                Type = type,
                OwnerId = ownerId,
                SensitivityLevel = Math.Clamp(sensitivity, 1, 10),
                State = AssetState.PROTECTED,
                ProtectedAt = DateTime.UtcNow
            };

            _assets[asset.Id] = asset;
            PersistUnlocked();
            _logger.LogInformation("Registered protected asset {Path}", full);
            return Task.FromResult(Clone(asset));
        }
    }

    public Task UnregisterAsync(string assetId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (_assets.Remove(assetId))
            {
                PersistUnlocked();
                _logger.LogInformation("Unregistered protected asset {AssetId}", assetId);
            }
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ProtectedAsset>> GetAssetsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<ProtectedAsset>>(_assets.Values.Select(Clone).ToList());
        }
    }

    public Task<ProtectedAsset?> GetByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(path)) return Task.FromResult<ProtectedAsset?>(null);
        var full = Path.GetFullPath(path);
        lock (_sync)
        {
            var match = _assets.Values.FirstOrDefault(a =>
                string.Equals(a.Path, full, StringComparison.OrdinalIgnoreCase) ||
                full.StartsWith(a.Path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(match is null ? null : Clone(match));
        }
    }

    public bool IsProtected(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var full = Path.GetFullPath(path);
        lock (_sync)
        {
            return _assets.Values.Any(a =>
                string.Equals(a.Path, full, StringComparison.OrdinalIgnoreCase) ||
                full.StartsWith(a.Path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase));
        }
    }

    public Task RestrictAssetsAsync(string incidentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(incidentId);

        lock (_sync)
        {
            var changed = 0;
            foreach (var asset in _assets.Values)
            {
                if (asset.State == AssetState.RESTRICTED)
                    continue;

                asset.State = AssetState.RESTRICTED;
                asset.LastModified = DateTime.UtcNow;
                asset.Metadata["RestrictedAt"] = DateTime.UtcNow.ToString("O");
                asset.Metadata["IncidentId"] = incidentId;
                changed++;
            }

            if (changed > 0)
            {
                PersistUnlocked();
                _logger.LogWarning("Restricted {Count} assets for incident {IncidentId}", changed, incidentId);
            }
        }

        return Task.CompletedTask;
    }

    public Task RestoreAssetsAsync(string incidentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(incidentId);

        lock (_sync)
        {
            var changed = 0;
            foreach (var asset in _assets.Values)
            {
                if (asset.State != AssetState.RESTRICTED)
                    continue;

                if (!asset.Metadata.TryGetValue("IncidentId", out var idObj))
                    continue;

                var id = idObj?.ToString();
                if (!string.Equals(id, incidentId, StringComparison.OrdinalIgnoreCase))
                    continue;

                asset.State = AssetState.PROTECTED;
                asset.LastModified = DateTime.UtcNow;
                asset.Metadata.Remove("RestrictedAt");
                asset.Metadata.Remove("IncidentId");
                changed++;
            }

            if (changed > 0)
            {
                PersistUnlocked();
                _logger.LogInformation("Restored {Count} assets for incident {IncidentId}", changed, incidentId);
            }
        }

        return Task.CompletedTask;
    }

    private void Load()
    {
        if (!File.Exists(_storePath)) return;
        try
        {
            var json = File.ReadAllText(_storePath);
            var list = JsonSerializer.Deserialize<List<ProtectedAsset>>(json, JsonOptions) ?? [];
            lock (_sync)
            {
                _assets.Clear();
                foreach (var asset in list)
                    _assets[asset.Id] = asset;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load protected assets from {Path}", _storePath);
        }
    }

    private void PersistUnlocked()
    {
        var json = JsonSerializer.Serialize(_assets.Values.ToList(), JsonOptions);
        var temp = _storePath + ".tmp";
        File.WriteAllText(temp, json);
        File.Copy(temp, _storePath, overwrite: true);
        File.Delete(temp);
    }

    private static ProtectedAsset Clone(ProtectedAsset source)
        => JsonSerializer.Deserialize<ProtectedAsset>(JsonSerializer.Serialize(source, JsonOptions), JsonOptions)!;
}

