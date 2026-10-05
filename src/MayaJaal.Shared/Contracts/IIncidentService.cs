using MayaJaal.Shared.Models;

namespace MayaJaal.Shared.Contracts;

public interface IIncidentService
{
    Task<Incident> CreateIncidentAsync(
        ThreatLevel level,
        int riskScore,
        double confidence,
        string primarySignal,
        IEnumerable<SecurityEvent> events,
        ResponseAction action,
        CancellationToken cancellationToken = default);

    Task ExecuteResponseAsync(Incident incident, ResponseAction action, CancellationToken cancellationToken = default);
    Task PreserveEvidenceAsync(Incident incident, CancellationToken cancellationToken = default);
    Task<Incident?> GetActiveIncidentAsync(CancellationToken cancellationToken = default);
    Task<Incident?> GetIncidentAsync(string incidentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Incident>> ListIncidentsAsync(int count = 50, CancellationToken cancellationToken = default);
    Task ResolveIncidentAsync(string incidentId, string resolvedBy, string? notes = null, CancellationToken cancellationToken = default);
    Task ExecuteRollbackAsync(string incidentId, CancellationToken cancellationToken = default);
}

