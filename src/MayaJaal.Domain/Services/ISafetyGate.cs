using MayaJaal.Shared.Models;

namespace MayaJaal.Domain.Services;

public interface ISafetyGate
{
    SafetyResult ValidateAction(ResponseAction action, ThreatContext context);
    bool IsActionSafe(ResponseAction action, ThreatContext context);
    List<SafetyCheck> GetSafetyChecks(ResponseAction action);
    RollbackPlan GetRollbackPlan(ResponseAction action);
    bool IsReversible(ResponseAction action);
}

public sealed class SafetyResult
{
    public bool IsApproved { get; set; }
    public List<SafetyWarning> Warnings { get; set; } = new();
    public List<SafetyCheck> Checks { get; set; } = new();
    public string DecisionReason { get; set; } = string.Empty;
    public int SafetyScore { get; set; }
}

public sealed class SafetyCheck
{
    public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Details { get; set; } = string.Empty;
    public int Severity { get; set; } = 1;
}

public sealed class SafetyWarning
{
    public string Message { get; set; } = string.Empty;
    public SafetyWarningType Type { get; set; }
    public string? Mitigation { get; set; }
}

public enum SafetyWarningType
{
    PRECONDITION_FAILED,
    AUTHORIZATION_FAILED,
    IRREVERSIBLE_ACTION,
    HIGH_IMPACT_ACTION,
    CONFIGURATION_RISK
}

public sealed class RollbackPlan
{
    public List<RollbackStep> Steps { get; set; } = new();
    public int EstimatedDurationSeconds { get; set; }
    public List<string> Prerequisites { get; set; } = new();
    public bool IsAutomated { get; set; } = true;
}

public sealed class RollbackStep
{
    public int Order { get; set; }
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IsRequired { get; set; } = true;
}
