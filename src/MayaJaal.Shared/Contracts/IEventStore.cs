using MayaJaal.Shared.Models;

namespace MayaJaal.Shared.Contracts;

public interface IEventStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task StoreEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityEvent>> GetRecentEventsAsync(int count = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityEvent>> GetEventsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityEvent>> GetEventsByTypeAsync(EventType type, int count = 100, CancellationToken cancellationToken = default);
    Task<long> GetEventCountAsync(CancellationToken cancellationToken = default);
    Task<string?> GetLastHashAsync(CancellationToken cancellationToken = default);
}
