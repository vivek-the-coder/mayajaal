using MayaJaal.Shared.Models;

namespace MayaJaal.Shared.Contracts;

public interface IVaultService
{
    Task<Vault> CreateVaultAsync(string name, string password, string ownerId, CancellationToken cancellationToken = default);
    Task<bool> UnlockVaultAsync(string vaultId, string password, CancellationToken cancellationToken = default);
    Task LockVaultAsync(string vaultId, CancellationToken cancellationToken = default);
    Task LockAllAsync(CancellationToken cancellationToken = default);
    Task<VaultItem> AddItemAsync(string vaultId, string sourcePath, CancellationToken cancellationToken = default);
    Task RemoveItemAsync(string vaultId, string itemId, CancellationToken cancellationToken = default);
    Task<byte[]> DecryptItemAsync(string vaultId, string itemId, CancellationToken cancellationToken = default);
    Task<Vault?> GetVaultAsync(string vaultId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vault>> ListVaultsAsync(CancellationToken cancellationToken = default);
    bool IsUnlocked(string vaultId);
}
