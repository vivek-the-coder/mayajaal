using MayaJaal.Domain.Engines;
using MayaJaal.Domain.Services;
using MayaJaal.Shared.Models;
using Xunit;

namespace MayaJaal.Tests.UnitTests;

public class PolicyEngineTests
{
    private readonly PolicyEngine _engine = new();

    [Fact]
    public void DetermineResponse_CriticalHighConfidence_ReturnsEmergencyLockdown()
    {
        var risk = new RiskScore
        {
            Value = 180,
            Confidence = 0.9,
            ThreatLevel = ThreatLevel.CRITICAL
        };

        var action = _engine.DetermineResponse(new(), risk);

        Assert.Equal(ResponseAction.EMERGENCY_LOCKDOWN, action.Action);
        Assert.True(action.IsReversible);
        Assert.Equal(4, action.SafetyLevel);
    }

    [Fact]
    public void DetermineResponse_HighConfidence_ReturnsContain()
    {
        var risk = new RiskScore
        {
            Value = 100,
            Confidence = 0.75,
            ThreatLevel = ThreatLevel.HIGH
        };

        var action = _engine.DetermineResponse(new(), risk);

        Assert.Equal(ResponseAction.CONTAIN, action.Action);
        Assert.True(action.IsReversible);
    }

    [Fact]
    public void DetermineResponse_HighLowConfidence_ReturnsLockVault()
    {
        var risk = new RiskScore
        {
            Value = 100,
            Confidence = 0.5,
            ThreatLevel = ThreatLevel.HIGH
        };

        var action = _engine.DetermineResponse(new(), risk);

        Assert.Equal(ResponseAction.LOCK_VAULT, action.Action);
    }

    [Fact]
    public void DetermineResponse_Medium_ReturnsAlert()
    {
        var risk = new RiskScore
        {
            Value = 60,
            Confidence = 0.55,
            ThreatLevel = ThreatLevel.MEDIUM
        };

        var action = _engine.DetermineResponse(new(), risk);

        Assert.Equal(ResponseAction.ALERT, action.Action);
    }

    [Fact]
    public void DetermineResponse_Low_ReturnsMonitor()
    {
        var risk = new RiskScore
        {
            Value = 30,
            Confidence = 0.4,
            ThreatLevel = ThreatLevel.LOW
        };

        var action = _engine.DetermineResponse(new(), risk);

        Assert.Equal(ResponseAction.MONITOR, action.Action);
    }

    [Fact]
    public void Evaluate_CriticalWindow_TriggersCriticalContainRule()
    {
        var risk = new RiskScore
        {
            Value = 150,
            Confidence = 0.9,
            ThreatLevel = ThreatLevel.CRITICAL,
            Factors = [new RiskFactor { Name = "HONEY_ACCESS", Contribution = 70 }]
        };

        var result = _engine.Evaluate(Array.Empty<SecurityEvent>(), risk);

        Assert.True(result.IsTriggered);
        Assert.Equal(ResponseAction.EMERGENCY_LOCKDOWN, result.Action.Action);
        Assert.Contains("CRITICAL_CONTAIN", result.TriggeredRules);
        Assert.False(string.IsNullOrWhiteSpace(result.Explanation));
    }

    [Fact]
    public void GetDefaultPolicy_HasRiskGatedRules()
    {
        var policy = _engine.GetDefaultPolicy();

        Assert.True(policy.IsEnabled);
        Assert.Equal(ResponseAction.MONITOR, policy.DefaultAction.Action);
        Assert.True(policy.Rules.Count >= 3);
        Assert.Contains(policy.Rules, r => r.Action.Action == ResponseAction.EMERGENCY_LOCKDOWN);
    }
}
