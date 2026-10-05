using MayaJaal.Domain.Services;
using MayaJaal.Shared.Models;

namespace MayaJaal.Domain.Engines;

public sealed class PolicyEngine : IPolicyEngine
{
    private readonly Policy _defaultPolicy;

    public PolicyEngine()
    {
        _defaultPolicy = GetDefaultPolicy();
    }

    public PolicyEvaluationResult Evaluate(IEnumerable<SecurityEvent> events, RiskScore risk)
    {
        var list = events.ToList();
        var context = new ThreatContext
        {
            Events = list,
            CurrentRisk = risk,
            CurrentThreatLevel = risk.ThreatLevel,
            UserId = list.FirstOrDefault()?.User?.UserId
        };

        var action = DetermineResponse(context, risk);
        var triggered = new List<string>();

        if (risk.Value >= 121 && risk.Confidence >= 0.85)
            triggered.Add("CRITICAL_CONTAIN");
        else if (risk.Value >= 81 && risk.Confidence >= 0.7)
            triggered.Add("HIGH_PRECONTAIN");
        else if (risk.Value >= 51 && risk.Confidence >= 0.5)
            triggered.Add("MEDIUM_ALERT");
        else if (risk.Value >= 21)
            triggered.Add("LOW_MONITOR");

        if (list.Any(e => e.IsHoney))
            triggered.Add("HONEY_SIGNAL");

        var isTriggered = action.Action != ResponseAction.NONE;
        return new PolicyEvaluationResult
        {
            IsTriggered = isTriggered,
            Action = action,
            TriggeredRules = triggered,
            Confidence = risk.Confidence,
            Explanation = BuildExplanation(risk, action, triggered),
            Evidence = new Dictionary<string, object>
            {
                ["risk"] = risk.Value,
                ["confidence"] = risk.Confidence,
                ["threatLevel"] = risk.ThreatLevel.ToString()
            }
        };
    }

    public List<Policy> GetApplicablePolicies(ThreatContext context)
        => [_defaultPolicy];

    public PolicyAction DetermineResponse(ThreatContext context, RiskScore risk)
    {
        var level = risk.ThreatLevel;
        var confidence = risk.Confidence;

        return level switch
        {
            ThreatLevel.CRITICAL when confidence >= 0.85 => new PolicyAction
            {
                Action = ResponseAction.EMERGENCY_LOCKDOWN,
                IsReversible = true,
                SafetyLevel = 4,
                Parameters = new Dictionary<string, object> { ["lockVault"] = true, ["preserveEvidence"] = true }
            },
            ThreatLevel.HIGH when confidence >= 0.7 => new PolicyAction
            {
                Action = ResponseAction.CONTAIN,
                IsReversible = true,
                SafetyLevel = 3,
                Parameters = new Dictionary<string, object> { ["lockVault"] = true, ["restrictAssets"] = true }
            },
            ThreatLevel.HIGH => new PolicyAction
            {
                Action = ResponseAction.LOCK_VAULT,
                IsReversible = true,
                SafetyLevel = 3
            },
            ThreatLevel.MEDIUM when confidence >= 0.5 => new PolicyAction
            {
                Action = ResponseAction.ALERT,
                IsReversible = true,
                SafetyLevel = 1
            },
            ThreatLevel.LOW => new PolicyAction
            {
                Action = ResponseAction.MONITOR,
                IsReversible = true,
                SafetyLevel = 0
            },
            _ => new PolicyAction { Action = ResponseAction.NONE, SafetyLevel = 0 }
        };
    }

    public bool IsPolicyViolated(Policy policy, ThreatContext context)
    {
        if (!policy.IsEnabled || context.CurrentRisk is null) return false;
        return context.CurrentRisk.Value >= 51 && context.CurrentRisk.Confidence >= 0.5;
    }

    public Policy GetDefaultPolicy() => new()
    {
        Name = "MayaJaal Default Defense Policy",
        Description = "Risk + confidence gated responses with reversible containment",
        Priority = 100,
        DefaultAction = new PolicyAction { Action = ResponseAction.MONITOR, SafetyLevel = 0 },
        Config = new Dictionary<string, object>
        {
            ["riskThreshold"] = 51,
            ["confidenceThreshold"] = 0.5,
            ["assetSensitivityMin"] = 5
        },
        Rules =
        [
            new PolicyRule
            {
                Condition = "risk >= 121 AND confidence >= 0.85",
                Action = new PolicyAction { Action = ResponseAction.EMERGENCY_LOCKDOWN, SafetyLevel = 4 },
                Priority = 1
            },
            new PolicyRule
            {
                Condition = "risk >= 81 AND confidence >= 0.70",
                Action = new PolicyAction { Action = ResponseAction.CONTAIN, SafetyLevel = 3 },
                Priority = 2
            },
            new PolicyRule
            {
                Condition = "risk >= 51 AND confidence >= 0.50",
                Action = new PolicyAction { Action = ResponseAction.ALERT, SafetyLevel = 1 },
                Priority = 3
            }
        ]
    };

    private static string BuildExplanation(RiskScore risk, PolicyAction action, List<string> rules)
    {
        var factors = string.Join(", ", risk.Factors.OrderByDescending(f => f.Contribution).Take(3).Select(f => $"{f.Name}(+{f.Contribution})"));
        return $"Threat={risk.ThreatLevel}, Risk={risk.Value}, Confidence={risk.Confidence:P0}. " +
               $"Action={action.Action}. Rules=[{string.Join(", ", rules)}]. Top factors: {factors}";
    }
}
