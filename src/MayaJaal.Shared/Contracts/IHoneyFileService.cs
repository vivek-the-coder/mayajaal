using MayaJaal.Shared.Models;

namespace MayaJaal.Shared.Contracts;

public interface IHoneyFileService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HoneyFile>> DeployDecoysAsync(string? decoyRoot = null, int count = 6, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HoneyFile>> GetHoneyFilesAsync(CancellationToken cancellationToken = default);
    bool IsHoneyPath(string path);
    HoneyFile? FindByPath(string path);
}
