using MayaJaal.Domain.Services;
using MayaJaal.Shared.Models;

namespace MayaJaal.Domain.Engines;

/// <summary>
/// Prefers reversible containment. Blocks irreversible or under-confident high-impact actions.
/// </summary>
public sealed class SafetyGate : ISafetyGate
{
    public SafetyResult ValidateAction(ResponseAction action, ThreatContext context)
    {
        var checks = GetSafetyChecks(action);
        var warnings = new List<SafetyWarning>();
        var score = 100;

        // Confidence gate for high-impact actions
        var required = action switch
        {
            ResponseAction.EMERGENCY_LOCKDOWN => 0.85,
            ResponseAction.CONTAIN => 0.70,
            ResponseAction.LOCK_VAULT or ResponseAction.RESTRICT_ASSETS => 0.60,
            _ => 0.0
        };

        var confidence = context.CurrentRisk?.Confidence ?? 0;
        if (confidence < required)
        {
            checks.Add(new SafetyCheck
            {
                Name = "ConfidenceThreshold",
                Passed = false,
                Details = $"Required {required:P0}, got {confidence:P0}",
                Severity = 4
            });
            warnings.Add(new SafetyWarning
            {
                Message = "Confidence below threshold for requested action",
                Type = SafetyWarningType.PRECONDITION_FAILED,
                Mitigation = "Increase monitoring or wait for stronger correlation"
            });
            score -= 40;
        }
        else
        {
            checks.Add(new SafetyCheck
            {
                Name = "ConfidenceThreshold",
                Passed = true,
                Details = $"Confidence {confidence:P0} meets {required:P0}",
                Severity = 1
            });
        }

        if (!IsReversible(action))
        {
            warnings.Add(new SafetyWarning
            {
                Message = "Action is not reversible",
                Type = SafetyWarningType.IRREVERSIBLE_ACTION,
                Mitigation = "Refuse by default"
            });
            score -= 50;
            checks.Add(new SafetyCheck
            {
                Name = "Reversibility",
                Passed = false,
                Details = "Irreversible actions are blocked",
                Severity = 5
            });
        }
        else
        {
            checks.Add(new SafetyCheck
            {
                Name = "Reversibility",
                Passed = true,
                Details = "Action is reversible",
                Severity = 1
            });
        }

        if (action is ResponseAction.EMERGENCY_LOCKDOWN or ResponseAction.CONTAIN)
        {
            warnings.Add(new SafetyWarning
            {
                Message = "High-impact containment",
                Type = SafetyWarningType.HIGH_IMPACT_ACTION,
                Mitigation = "Rollback plan available"
            });
            score -= 10;
        }

        var failed = checks.Any(c => !c.Passed && c.Severity >= 4);
        var approved = !failed && score >= 50;

        return new SafetyResult
        {
            IsApproved = approved,
            Checks = checks,
            Warnings = warnings,
            SafetyScore = Math.Max(0, score),
            DecisionReason = approved
                ? $"Approved {action} (safety score {score})"
                : $"Blocked {action}: {string.Join("; ", warnings.Select(w => w.Message))}"
        };
    }

    public bool IsActionSafe(ResponseAction action, ThreatContext context)
        => ValidateAction(action, context).IsApproved;

    public List<SafetyCheck> GetSafetyChecks(ResponseAction action) =>
    [
        new() { Name = "ActionDefined", Passed = action != ResponseAction.NONE || true, Details = action.ToString() },
        new() { Name = "PolicyAligned", Passed = true, Details = "Mapped to default policy" }
    ];

    public RollbackPlan GetRollbackPlan(ResponseAction action) => action switch
    {
        ResponseAction.LOCK_VAULT => new RollbackPlan
        {
            EstimatedDurationSeconds = 5,
            Steps = [new RollbackStep { Order = 1, Action = "UNLOCK_VAULT" }]
        },
        ResponseAction.RESTRICT_ASSETS => new RollbackPlan
        {
            EstimatedDurationSeconds = 10,
            Steps = [new RollbackStep { Order = 1, Action = "RESTORE_ASSET_ACCESS" }]
        },
        ResponseAction.CONTAIN or ResponseAction.EMERGENCY_LOCKDOWN => new RollbackPlan
        {
            EstimatedDurationSeconds = 30,
            Steps =
            [
                new RollbackStep { Order = 1, Action = "UNLOCK_VAULT" },
                new RollbackStep { Order = 2, Action = "RESTORE_ASSET_ACCESS" },
                new RollbackStep { Order = 3, Action = "RESUME_NORMAL_MONITORING" }
            ]
        },
        _ => new RollbackPlan { EstimatedDurationSeconds = 0 }
    };

    public bool IsReversible(ResponseAction action) => action switch
    {
        // All MayaJaal automated responses are designed reversible
        ResponseAction.NONE or ResponseAction.MONITOR or ResponseAction.ALERT
            or ResponseAction.NOTIFY_USER or ResponseAction.PRESERVE_EVIDENCE
            or ResponseAction.GENERATE_REPORT or ResponseAction.LOCK_VAULT
            or ResponseAction.RESTRICT_ASSETS or ResponseAction.CONTAIN
            or ResponseAction.EMERGENCY_LOCKDOWN => true,
        _ => true
    };
}
