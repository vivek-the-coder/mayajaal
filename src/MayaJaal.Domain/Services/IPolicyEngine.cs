using MayaJaal.Shared.Models;

namespace MayaJaal.Domain.Services;

public interface IPolicyEngine
{
    PolicyEvaluationResult Evaluate(IEnumerable<SecurityEvent> events, RiskScore risk);
    List<Policy> GetApplicablePolicies(ThreatContext context);
    PolicyAction DetermineResponse(ThreatContext context, RiskScore risk);
    bool IsPolicyViolated(Policy policy, ThreatContext context);
    Policy GetDefaultPolicy();
}

public sealed class Policy
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; }
    public List<PolicyRule> Rules { get; set; } = new();
    public PolicyAction DefaultAction { get; set; } = new();
    public Dictionary<string, object> Config { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string Version { get; set; } = "1.0";
}

public sealed class PolicyRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Condition { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public PolicyAction Action { get; set; } = new();
    public int Priority { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public sealed class PolicyAction
{
    public ResponseAction Action { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IsReversible { get; set; } = true;
    public int SafetyLevel { get; set; } = 1;
}

public sealed class PolicyEvaluationResult
{
    public bool IsTriggered { get; set; }
    public PolicyAction Action { get; set; } = new();
    public List<string> TriggeredRules { get; set; } = new();
    public double Confidence { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public Dictionary<string, object> Evidence { get; set; } = new();
}

public sealed class ThreatContext
{
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public string? ProcessName { get; set; }
    public string? TargetPath { get; set; }
    public List<ProtectedAsset> Assets { get; set; } = new();
    public List<SecurityEvent> Events { get; set; } = new();
    public RiskScore? CurrentRisk { get; set; }
    public ThreatLevel CurrentThreatLevel { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
