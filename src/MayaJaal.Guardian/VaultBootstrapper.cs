using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using MayaJaal.Shared.Contracts;

namespace MayaJaal.Guardian;

/// <summary>
/// Ensures a default vault exists and is unlocked in-process so LockAll is meaningful.
/// Bootstrap password is stored LocalMachine DPAPI-protected under ProgramData.
/// </summary>
internal static class VaultBootstrapper
{
    public static async Task EnsureDefaultVaultAsync(
        IVaultService vaultService,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var vaults = await vaultService.ListVaultsAsync(cancellationToken).ConfigureAwait(false);
        if (vaults.Count > 0)
        {
            var existing = vaults.FirstOrDefault(v =>
                string.Equals(v.Name, "Default Vault", StringComparison.OrdinalIgnoreCase)) ?? vaults[0];

            var secret = TryReadBootstrapPassword();
            if (!string.IsNullOrEmpty(secret) && !vaultService.IsUnlocked(existing.Id))
            {
                try
                {
                    await vaultService.UnlockVaultAsync(existing.Id, secret, cancellationToken).ConfigureAwait(false);
                    logger.LogInformation("Unlocked vault {VaultId} from bootstrap secret", existing.Id);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Could not unlock existing vault from bootstrap secret");
                }
            }

            return;
        }

        logger.LogInformation("No vault found — provisioning Default Vault");
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        var vault = await vaultService.CreateVaultAsync(
            "Default Vault",
            password,
            Environment.UserName,
            cancellationToken).ConfigureAwait(false);

        WriteBootstrapPassword(password);
        await vaultService.UnlockVaultAsync(vault.Id, password, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Default vault provisioned and unlocked in-process: {VaultId}", vault.Id);
    }

    private static string SecretPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "MayaJaal",
        "Config",
        "default-vault.dpapi");

    private static void WriteBootstrapPassword(string password)
    {
        var dir = Path.GetDirectoryName(SecretPath)!;
        Directory.CreateDirectory(dir);
        var bytes = Encoding.UTF8.GetBytes(password);
        var protectedBytes = ProtectedData.Protect(bytes, optionalEntropy: null, DataProtectionScope.LocalMachine);
        File.WriteAllBytes(SecretPath, protectedBytes);
        CryptographicOperations.ZeroMemory(bytes);
    }

    private static string? TryReadBootstrapPassword()
    {
        try
        {
            if (!File.Exists(SecretPath)) return null;
            var protectedBytes = File.ReadAllBytes(SecretPath);
            var bytes = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }
}
