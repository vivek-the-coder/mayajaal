using MayaJaal.Shared.Models;

namespace MayaJaal.Shared.Contracts;

public interface IThreatEngine
{
    Task ProcessEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);
    ThreatState GetCurrentState();
    Incident? GetActiveIncident();
    void ClearActiveIncident(string? incidentId = null);
    IReadOnlyList<SecurityEvent> GetRecentEvents(int count = 50);
    Task ReplayRecentEventsAsync(int count = 500, CancellationToken cancellationToken = default);
}

