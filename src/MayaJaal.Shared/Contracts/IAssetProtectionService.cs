using MayaJaal.Shared.Models;

namespace MayaJaal.Shared.Contracts;

public interface IAssetProtectionService
{
    Task<ProtectedAsset> RegisterAsync(string path, AssetType type, string ownerId, int sensitivity = 5, CancellationToken cancellationToken = default);
    Task UnregisterAsync(string assetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProtectedAsset>> GetAssetsAsync(CancellationToken cancellationToken = default);
    Task<ProtectedAsset?> GetByPathAsync(string path, CancellationToken cancellationToken = default);
    bool IsProtected(string path);
    Task RestrictAssetsAsync(string incidentId, CancellationToken cancellationToken = default);
    Task RestoreAssetsAsync(string incidentId, CancellationToken cancellationToken = default);
}

