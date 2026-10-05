using Microsoft.Extensions.Logging;
using MayaJaal.Domain.Services;
using MayaJaal.Shared.Contracts;
using MayaJaal.Shared.Models;

namespace MayaJaal.Guardian;

/// <summary>
/// Full detection loop: store → risk → confidence → correlate → policy → safety → respond.
/// </summary>
public sealed class ThreatEngine : IThreatEngine
{
    private readonly IEventStore _eventStore;
    private readonly IRiskEngine _riskEngine;
    private readonly IConfidenceEngine _confidenceEngine;
    private readonly ICorrelationEngine _correlationEngine;
    private readonly IPolicyEngine _policyEngine;
    private readonly ISafetyGate _safetyGate;
    private readonly IIncidentService _incidentService;
    private readonly ILogger<ThreatEngine> _logger;
    private readonly object _sync = new();
    private readonly List<SecurityEvent> _recent = [];
    private ThreatState _state = new() { GuardianOnline = true };
    private Incident? _activeIncident;
    private readonly TimeSpan _window = TimeSpan.FromMinutes(15);

    public ThreatEngine(
        IEventStore eventStore,
        IRiskEngine riskEngine,
        IConfidenceEngine confidenceEngine,
        ICorrelationEngine correlationEngine,
        IPolicyEngine policyEngine,
        ISafetyGate safetyGate,
        IIncidentService incidentService,
        ILogger<ThreatEngine> logger)
    {
        _eventStore = eventStore;
        _riskEngine = riskEngine;
        _confidenceEngine = confidenceEngine;
        _correlationEngine = correlationEngine;
        _policyEngine = policyEngine;
        _safetyGate = safetyGate;
        _incidentService = incidentService;
        _logger = logger;
    }

    public async Task ProcessEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(securityEvent);
        if (securityEvent.Timestamp == default)
            securityEvent.Timestamp = DateTime.UtcNow;

        if (ProcessAllowlist.IsAllowed(securityEvent))
        {
            _logger.LogDebug("Allowlisted event skipped: {Type} {Process}",
                securityEvent.Type, securityEvent.Process?.ProcessName);
            return;
        }

        await _eventStore.StoreEventAsync(securityEvent, cancellationToken).ConfigureAwait(false);

        List<SecurityEvent> window;
        lock (_sync)
        {
            _recent.Add(securityEvent);
            var cutoff = DateTime.UtcNow - _window;
            _recent.RemoveAll(e => e.Timestamp < cutoff);
            if (_recent.Count > 500)
                _recent.RemoveRange(0, _recent.Count - 500);
            window = _recent.ToList();
        }

        // Heuristic ransomware emitter from recent mass file activity.
        if (securityEvent.Type is not EventType.RANSOMWARE_BEHAVIOR)
        {
            var ransomware = TryBuildRansomwareEvent(window);
            if (ransomware is not null)
                await ProcessEventAsync(ransomware, cancellationToken).ConfigureAwait(false);
        }

        var risk = _riskEngine.CalculateRisk(window);
        var confidence = _confidenceEngine.CalculateConfidence(window);
        risk.Confidence = Math.Max(risk.Confidence, confidence);

        var correlation = _correlationEngine.CorrelateEvents(window);
        if (correlation.IsCorrelated && correlation.Evidence.TryGetValue("typicalRisk", out var typical))
        {
            var boost = Convert.ToInt32(typical) / 10;
            risk.Value = Math.Min(200, risk.Value + boost);
            risk.Confidence = Math.Min(1.0, risk.Confidence * Math.Max(1.0, correlation.CorrelationStrength + 0.5));
            risk.ThreatLevel = _riskEngine.DetermineThreatLevel(risk.Value, risk.Confidence);
        }

        var evaluation = _policyEngine.Evaluate(window, risk);
        var context = new ThreatContext
        {
            Events = window,
            CurrentRisk = risk,
            CurrentThreatLevel = risk.ThreatLevel,
            UserId = securityEvent.User?.UserId,
            ProcessName = securityEvent.Process?.ProcessName,
            TargetPath = securityEvent.Path
        };

        Incident? incident = null;
        if (evaluation.IsTriggered && evaluation.Action.Action != ResponseAction.NONE)
        {
            var safety = _safetyGate.ValidateAction(evaluation.Action.Action, context);
            if (safety.IsApproved)
            {
                var primary = correlation.PatternType
                    ?? securityEvent.Type.ToString();

                incident = await _incidentService.GetActiveIncidentAsync(cancellationToken).ConfigureAwait(false);
                if (incident is null ||
                    incident.Status is IncidentStatus.RESOLVED or IncidentStatus.CLOSED or IncidentStatus.ARCHIVED)
                {
                    incident = await _incidentService.CreateIncidentAsync(
                        risk.ThreatLevel,
                        risk.Value,
                        risk.Confidence,
                        primary,
                        window,
                        evaluation.Action.Action,
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    incident.RiskScore = Math.Max(incident.RiskScore, risk.Value);
                    incident.Confidence = Math.Max(incident.Confidence, risk.Confidence);
                    incident.Level = risk.ThreatLevel;
                    incident.Events = window;
                    incident.Action = evaluation.Action.Action;
                    incident.Timeline.Add(new TimelineEntry
                    {
                        Timestamp = DateTime.UtcNow,
                        EventType = securityEvent.Type.ToString(),
                        Description = evaluation.Explanation,
                        RiskDelta = securityEvent.RiskContribution,
                        Target = securityEvent.Path
                    });
                }

                await _incidentService.ExecuteResponseAsync(incident, evaluation.Action.Action, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogWarning(
                    "Threat response {Action} for {Pattern} (risk={Risk}, conf={Conf:P0})",
                    evaluation.Action.Action,
                    primary,
                    risk.Value,
                    risk.Confidence);
            }
            else
            {
                _logger.LogInformation(
                    "SafetyGate blocked {Action}: {Reason}",
                    evaluation.Action.Action,
                    safety.DecisionReason);
            }
        }

        lock (_sync)
        {
            _activeIncident = incident ?? _activeIncident;
            _state = new ThreatState
            {
                CurrentRisk = risk.Value,
                Confidence = risk.Confidence,
                Level = risk.ThreatLevel,
                RecentEvents = window.TakeLast(25).ToList(),
                ActiveIncident = _activeIncident,
                GuardianOnline = true,
                EventCount = window.Count,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public ThreatState GetCurrentState()
    {
        lock (_sync)
        {
            return new ThreatState
            {
                CurrentRisk = _state.CurrentRisk,
                Confidence = _state.Confidence,
                Level = _state.Level,
                RecentEvents = _state.RecentEvents.ToList(),
                ActiveIncident = _activeIncident,
                GuardianOnline = true,
                EventCount = _state.EventCount,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public Incident? GetActiveIncident()
    {
        lock (_sync) return _activeIncident;
    }

    public void ClearActiveIncident(string? incidentId = null)
    {
        lock (_sync)
        {
            if (incidentId is not null &&
                _activeIncident is not null &&
                !string.Equals(_activeIncident.Id, incidentId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _activeIncident = null;
            _state = new ThreatState
            {
                CurrentRisk = _state.CurrentRisk,
                Confidence = _state.Confidence,
                Level = _state.Level,
                RecentEvents = _state.RecentEvents.ToList(),
                ActiveIncident = null,
                GuardianOnline = true,
                EventCount = _state.EventCount,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public IReadOnlyList<SecurityEvent> GetRecentEvents(int count = 50)
    {
        lock (_sync)
        {
            return _recent.TakeLast(Math.Clamp(count, 1, 500)).ToList();
        }
    }

    public async Task ReplayRecentEventsAsync(int count = 500, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 2000);
        var events = await _eventStore.GetRecentEventsAsync(count, cancellationToken).ConfigureAwait(false);
        var ordered = events.OrderBy(e => e.Timestamp).ThenBy(e => e.Sequence).ToList();

        lock (_sync)
        {
            _recent.Clear();
            var cutoff = DateTime.UtcNow - _window;
            foreach (var evt in ordered.Where(e => e.Timestamp >= cutoff).TakeLast(500))
                _recent.Add(evt);

            if (_recent.Count == 0 && ordered.Count > 0)
            {
                // If all events are outside the live window, keep the newest slice for UI continuity.
                _recent.AddRange(ordered.TakeLast(Math.Min(100, ordered.Count)));
            }

            if (_recent.Count > 0)
            {
                var risk = _riskEngine.CalculateRisk(_recent);
                var confidence = _confidenceEngine.CalculateConfidence(_recent);
                _state = new ThreatState
                {
                    CurrentRisk = risk.Value,
                    Confidence = Math.Max(risk.Confidence, confidence),
                    Level = risk.ThreatLevel,
                    RecentEvents = _recent.TakeLast(25).ToList(),
                    ActiveIncident = _activeIncident,
                    GuardianOnline = true,
                    EventCount = _recent.Count,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        _logger.LogInformation("Replayed {Count} events from SQLite into ThreatEngine window", ordered.Count);
    }

    private static SecurityEvent? TryBuildRansomwareEvent(IReadOnlyList<SecurityEvent> window)
    {
        var now = DateTime.UtcNow;
        var recentMods = window.Count(e =>
            (e.Type is EventType.FILE_MODIFY or EventType.FILE_DELETE or EventType.MASS_FILE_ACTIVITY) &&
            (now - e.Timestamp).TotalSeconds < 15);

        var suspiciousRename = window.Any(e =>
            e.Type == EventType.FILE_RENAME &&
            (e.Path?.EndsWith(".encrypted", StringComparison.OrdinalIgnoreCase) == true ||
             e.Path?.EndsWith(".locked", StringComparison.OrdinalIgnoreCase) == true ||
             e.Path?.EndsWith(".crypto", StringComparison.OrdinalIgnoreCase) == true ||
             e.Path?.EndsWith(".ransom", StringComparison.OrdinalIgnoreCase) == true));

        if (recentMods < 40 && !(recentMods >= 15 && suspiciousRename))
            return null;

        // Avoid spamming: only emit if no ransomware event already in the last minute.
        if (window.Any(e => e.Type == EventType.RANSOMWARE_BEHAVIOR && (now - e.Timestamp).TotalSeconds < 60))
            return null;

        return new SecurityEvent
        {
            Type = EventType.RANSOMWARE_BEHAVIOR,
            Source = EventSource.SYSTEM,
            Severity = EventSeverity.CRITICAL,
            RiskContribution = 80,
            Timestamp = now,
            Metadata =
            {
                ["modifications"] = recentMods,
                ["suspiciousRename"] = suspiciousRename
            }
        };
    }
}

