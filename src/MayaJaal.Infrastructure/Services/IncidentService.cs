using System.Text.Json;
using Microsoft.Extensions.Logging;
using MayaJaal.Domain.Services;
using MayaJaal.Infrastructure.Reporting;
using MayaJaal.Shared.Contracts;
using MayaJaal.Shared.Models;

namespace MayaJaal.Infrastructure.Services;

public sealed class IncidentService : IIncidentService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IVaultService _vaultService;
    private readonly IAssetProtectionService _assetProtection;
    private readonly ISafetyGate _safetyGate;
    private readonly ReportGenerator _reportGenerator;
    private readonly ILogger<IncidentService> _logger;
    private readonly string _incidentsRoot;
    private readonly string _evidenceRoot;
    private readonly object _sync = new();
    private Incident? _active;
    private int _incidentCounter;

    public IncidentService(
        IVaultService vaultService,
        IAssetProtectionService assetProtection,
        ISafetyGate safetyGate,
        ReportGenerator reportGenerator,
        ILogger<IncidentService> logger,
        string? dataRoot = null)
    {
        _vaultService = vaultService;
        _assetProtection = assetProtection;
        _safetyGate = safetyGate;
        _reportGenerator = reportGenerator;
        _logger = logger;
        var root = dataRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "MayaJaal");
        _incidentsRoot = Path.Combine(root, "Incidents");
        _evidenceRoot = Path.Combine(root, "Evidence");
        Directory.CreateDirectory(_incidentsRoot);
        Directory.CreateDirectory(_evidenceRoot);
        _incidentCounter = Directory.Exists(_incidentsRoot)
            ? Directory.GetDirectories(_incidentsRoot).Length
            : 0;
    }

    public async Task<Incident> CreateIncidentAsync(
        ThreatLevel level,
        int riskScore,
        double confidence,
        string primarySignal,
        IEnumerable<SecurityEvent> events,
        ResponseAction action,
        CancellationToken cancellationToken = default)
    {
        var eventList = events.ToList();
        var number = Interlocked.Increment(ref _incidentCounter);
        var incident = new Incident
        {
            Number = $"INC-{DateTime.UtcNow:yyyyMMdd}-{number:D4}",
            Level = level,
            RiskScore = riskScore,
            Confidence = confidence,
            PrimarySignal = primarySignal,
            Action = action,
            Status = IncidentStatus.OPEN,
            Events = eventList,
            Timeline =
            [
                new TimelineEntry
                {
                    Timestamp = DateTime.UtcNow,
                    EventType = "INCIDENT_CREATED",
                    Description = $"Incident opened: {primarySignal}",
                    RiskDelta = riskScore,
                    Details = new Dictionary<string, object>
                    {
                        ["confidence"] = confidence,
                        ["action"] = action.ToString()
                    }
                }
            ]
        };

        lock (_sync) _active = incident;
        await PersistAsync(incident, cancellationToken).ConfigureAwait(false);
        await PreserveEvidenceAsync(incident, cancellationToken).ConfigureAwait(false);
        _logger.LogWarning(
            "Incident {Number} created at {Level} (risk={Risk}, confidence={Confidence:P0})",
            incident.Number, level, riskScore, confidence);
        return incident;
    }

    public async Task ExecuteResponseAsync(Incident incident, ResponseAction action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);
        incident.Action = action;
        incident.Status = IncidentStatus.CONTAINMENT_PENDING;

        incident.Timeline.Add(new TimelineEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = "RESPONSE_EXECUTE",
            Description = $"Executing reversible response: {action}",
            Actor = "MayaJaal.Guardian"
        });

        switch (action)
        {
            case ResponseAction.NONE:
            case ResponseAction.MONITOR:
                _logger.LogInformation("Incident {Number}: monitoring only", incident.Number);
                incident.Status = IncidentStatus.UNDER_ANALYSIS;
                break;

            case ResponseAction.ALERT:
            case ResponseAction.NOTIFY_USER:
                _logger.LogWarning("Incident {Number}: ALERT — {Signal}", incident.Number, incident.PrimarySignal);
                incident.Status = IncidentStatus.UNDER_ANALYSIS;
                break;

            case ResponseAction.LOCK_VAULT:
            case ResponseAction.CONTAIN:
            case ResponseAction.EMERGENCY_LOCKDOWN:
                _logger.LogWarning("Incident {Number}: locking vaults as part of {Action}", incident.Number, action);
                await _vaultService.LockAllAsync(cancellationToken).ConfigureAwait(false);
                if (action is ResponseAction.CONTAIN or ResponseAction.EMERGENCY_LOCKDOWN)
                    await _assetProtection.RestrictAssetsAsync(incident.Id, cancellationToken).ConfigureAwait(false);
                incident.Status = IncidentStatus.CONTAINED;
                incident.Timeline.Add(new TimelineEntry
                {
                    Timestamp = DateTime.UtcNow,
                    EventType = "VAULT_LOCK",
                    Description = "All unlocked vaults were locked (reversible).",
                    Actor = "IncidentService"
                });
                break;

            case ResponseAction.RESTRICT_ASSETS:
                _logger.LogWarning("Incident {Number}: restricting protected assets (no vault lock)", incident.Number);
                await _assetProtection.RestrictAssetsAsync(incident.Id, cancellationToken).ConfigureAwait(false);
                incident.Status = IncidentStatus.CONTAINED;
                incident.Timeline.Add(new TimelineEntry
                {
                    Timestamp = DateTime.UtcNow,
                    EventType = "ASSET_RESTRICT",
                    Description = "Protected assets marked RESTRICTED for this incident.",
                    Actor = "IncidentService"
                });
                break;

            case ResponseAction.PRESERVE_EVIDENCE:
            case ResponseAction.GENERATE_REPORT:
                await PreserveEvidenceAsync(incident, cancellationToken).ConfigureAwait(false);
                incident.Status = IncidentStatus.UNDER_ANALYSIS;
                break;

            default:
                _logger.LogInformation("Incident {Number}: unhandled action {Action} treated as monitor", incident.Number, action);
                incident.Status = IncidentStatus.UNDER_ANALYSIS;
                break;
        }

        await PreserveEvidenceAsync(incident, cancellationToken).ConfigureAwait(false);
        await PersistAsync(incident, cancellationToken).ConfigureAwait(false);
        lock (_sync) _active = incident;
    }

    public async Task PreserveEvidenceAsync(Incident incident, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);
        var dir = Path.Combine(_evidenceRoot, incident.Id);
        Directory.CreateDirectory(dir);

        var evidencePath = Path.Combine(dir, "incident.json");
        var json = JsonSerializer.Serialize(incident, JsonOptions);
        await File.WriteAllTextAsync(evidencePath, json, cancellationToken).ConfigureAwait(false);

        var eventsPath = Path.Combine(dir, "events.json");
        await File.WriteAllTextAsync(
            eventsPath,
            JsonSerializer.Serialize(incident.Events, JsonOptions),
            cancellationToken).ConfigureAwait(false);

        if (incident.Evidence.All(e => e.Path != evidencePath))
        {
            incident.Evidence.Add(new Evidence
            {
                Type = EvidenceType.EVENT_LOG,
                Path = evidencePath,
                Hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json))).ToLowerInvariant(),
                Status = IntegrityStatus.VERIFIED,
                Metadata = { ["kind"] = "incident-snapshot" }
            });
        }

        try
        {
            var pdfPath = await _reportGenerator.WriteIncidentPdfAsync(incident, dir, cancellationToken).ConfigureAwait(false);
            if (incident.Evidence.All(e => e.Path != pdfPath))
            {
                incident.Evidence.Add(new Evidence
                {
                    Type = EvidenceType.SYSTEM_INFO,
                    Path = pdfPath,
                    Hash = Convert.ToHexString(
                        System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(pdfPath, cancellationToken).ConfigureAwait(false)))
                        .ToLowerInvariant(),
                    Status = IntegrityStatus.VERIFIED,
                    Metadata = { ["kind"] = "incident-pdf" }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF report generation failed for incident {Number}", incident.Number);
        }

        incident.Timeline.Add(new TimelineEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = "EVIDENCE_PRESERVED",
            Description = $"Evidence written to {dir}",
            Target = dir
        });

        await PersistAsync(incident, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Evidence preserved for {Number} at {Path}", incident.Number, dir);
    }

    public Task<Incident?> GetActiveIncidentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (_active is not null &&
                _active.Status is not (IncidentStatus.RESOLVED or IncidentStatus.CLOSED or IncidentStatus.ARCHIVED))
            {
                return Task.FromResult<Incident?>(_active);
            }
        }

        return Task.FromResult<Incident?>(null);
    }

    public async Task<Incident?> GetIncidentAsync(string incidentId, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_incidentsRoot, incidentId, "incident.json");
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<Incident>(json, JsonOptions);
    }

    public async Task<IReadOnlyList<Incident>> ListIncidentsAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 500);
        if (!Directory.Exists(_incidentsRoot)) return Array.Empty<Incident>();

        var incidents = new List<Incident>();
        foreach (var dir in Directory.EnumerateDirectories(_incidentsRoot).OrderByDescending(d => d).Take(count))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.Combine(dir, "incident.json");
            if (!File.Exists(path)) continue;
            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            var incident = JsonSerializer.Deserialize<Incident>(json, JsonOptions);
            if (incident is not null) incidents.Add(incident);
        }

        return incidents;
    }

    public async Task ResolveIncidentAsync(string incidentId, string resolvedBy, string? notes = null, CancellationToken cancellationToken = default)
    {
        var incident = await GetIncidentAsync(incidentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Incident '{incidentId}' not found.");

        incident.Status = IncidentStatus.RESOLVED;
        incident.EndTime = DateTime.UtcNow;
        incident.ResolvedBy = resolvedBy;
        incident.ResolutionNotes = notes;
        incident.Timeline.Add(new TimelineEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = "INCIDENT_RESOLVED",
            Description = notes ?? "Resolved",
            Actor = resolvedBy
        });

        await PersistAsync(incident, cancellationToken).ConfigureAwait(false);
        lock (_sync)
        {
            if (_active?.Id == incidentId)
                _active = null;
        }
    }

    public async Task ExecuteRollbackAsync(string incidentId, CancellationToken cancellationToken = default)
    {
        var incident = await GetIncidentAsync(incidentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Incident '{incidentId}' not found.");

        var plan = _safetyGate.GetRollbackPlan(incident.Action);
        if (!plan.IsAutomated)
        {
            _logger.LogWarning("Rollback not automated for incident {Number} action {Action}", incident.Number, incident.Action);
            return;
        }

        foreach (var step in plan.Steps.OrderBy(s => s.Order))
        {
            switch (step.Action.ToUpperInvariant())
            {
                case "RESTORE_ASSET_ACCESS":
                    await _assetProtection.RestoreAssetsAsync(incident.Id, cancellationToken).ConfigureAwait(false);
                    incident.Timeline.Add(new TimelineEntry
                    {
                        Timestamp = DateTime.UtcNow,
                        EventType = "ROLLBACK_RESTORE_ASSETS",
                        Description = "Restored asset access for restricted assets.",
                        Actor = "IncidentService"
                    });
                    break;

                case "UNLOCK_VAULT":
                    // Unlock requires operator password — mark pending for UI; do not auto-unlock.
                    incident.Timeline.Add(new TimelineEntry
                    {
                        Timestamp = DateTime.UtcNow,
                        EventType = "ROLLBACK_UNLOCK_PENDING",
                        Description = "Vault unlock requires operator credentials in Security Center.",
                        Actor = "IncidentService"
                    });
                    break;

                case "RESUME_NORMAL_MONITORING":
                    incident.Timeline.Add(new TimelineEntry
                    {
                        Timestamp = DateTime.UtcNow,
                        EventType = "ROLLBACK_MONITORING",
                        Description = "Resumed normal monitoring posture.",
                        Actor = "IncidentService"
                    });
                    break;
            }
        }

        if (incident.Status is IncidentStatus.CONTAINED or IncidentStatus.CONTAINMENT_PENDING)
            incident.Status = IncidentStatus.UNDER_ANALYSIS;

        await PersistAsync(incident, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Rollback plan executed for incident {Number}", incident.Number);
    }

    private async Task PersistAsync(Incident incident, CancellationToken cancellationToken)
    {
        var dir = Path.Combine(_incidentsRoot, incident.Id);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "incident.json");
        var json = JsonSerializer.Serialize(incident, JsonOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }
}

