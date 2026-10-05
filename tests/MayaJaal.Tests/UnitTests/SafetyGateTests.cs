using MayaJaal.Domain.Engines;
using MayaJaal.Domain.Services;
using MayaJaal.Shared.Models;
using Xunit;

namespace MayaJaal.Tests.UnitTests;

public class SafetyGateTests
{
    private readonly SafetyGate _gate = new();

    [Fact]
    public void ValidateAction_EmergencyLockdown_ApprovedWhenConfident()
    {
        var context = new ThreatContext
        {
            CurrentRisk = new RiskScore { Value = 180, Confidence = 0.9, ThreatLevel = ThreatLevel.CRITICAL }
        };

        var result = _gate.ValidateAction(ResponseAction.EMERGENCY_LOCKDOWN, context);

        Assert.True(result.IsApproved);
        Assert.Contains(result.Checks, c => c.Name == "ConfidenceThreshold" && c.Passed);
        Assert.Contains(result.Checks, c => c.Name == "Reversibility" && c.Passed);
    }

    [Fact]
    public void ValidateAction_EmergencyLockdown_BlockedWhenUnderConfident()
    {
        var context = new ThreatContext
        {
            CurrentRisk = new RiskScore { Value = 180, Confidence = 0.5, ThreatLevel = ThreatLevel.CRITICAL }
        };

        var result = _gate.ValidateAction(ResponseAction.EMERGENCY_LOCKDOWN, context);

        Assert.False(result.IsApproved);
        Assert.Contains(result.Checks, c => c.Name == "ConfidenceThreshold" && !c.Passed);
        Assert.Contains("Blocked", result.DecisionReason);
    }

    [Fact]
    public void ValidateAction_Contain_RequiresSeventyPercentConfidence()
    {
        var blocked = _gate.ValidateAction(
            ResponseAction.CONTAIN,
            new ThreatContext { CurrentRisk = new RiskScore { Confidence = 0.6 } });
        var approved = _gate.ValidateAction(
            ResponseAction.CONTAIN,
            new ThreatContext { CurrentRisk = new RiskScore { Confidence = 0.7 } });

        Assert.False(blocked.IsApproved);
        Assert.True(approved.IsApproved);
    }

    [Fact]
    public void ValidateAction_Alert_ApprovedWithoutConfidenceGate()
    {
        var result = _gate.ValidateAction(
            ResponseAction.ALERT,
            new ThreatContext { CurrentRisk = new RiskScore { Confidence = 0.1 } });

        Assert.True(result.IsApproved);
    }

    [Fact]
    public void GetRollbackPlan_Containment_HasUnlockAndRestoreSteps()
    {
        var plan = _gate.GetRollbackPlan(ResponseAction.EMERGENCY_LOCKDOWN);

        Assert.True(plan.EstimatedDurationSeconds >= 30);
        Assert.Contains(plan.Steps, s => s.Action == "UNLOCK_VAULT");
        Assert.Contains(plan.Steps, s => s.Action == "RESTORE_ASSET_ACCESS");
        Assert.Contains(plan.Steps, s => s.Action == "RESUME_NORMAL_MONITORING");
    }

    [Fact]
    public void IsActionSafe_MatchesValidateApproval()
    {
        var context = new ThreatContext
        {
            CurrentRisk = new RiskScore { Confidence = 0.95 }
        };

        Assert.Equal(
            _gate.ValidateAction(ResponseAction.LOCK_VAULT, context).IsApproved,
            _gate.IsActionSafe(ResponseAction.LOCK_VAULT, context));
    }
}
