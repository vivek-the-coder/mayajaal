using MayaJaal.Domain.Engines;
using MayaJaal.Domain.Services;
using MayaJaal.Shared.Models;
using Xunit;

namespace MayaJaal.Tests.UnitTests;

public class RiskEngineTests
{
    private readonly RiskEngine _engine = new();

    [Fact]
    public void CalculateRisk_EmptyEvents_ReturnsSafeZero()
    {
        var score = _engine.CalculateRisk(Array.Empty<SecurityEvent>());

        Assert.Equal(0, score.Value);
        Assert.Equal(0, score.Confidence);
        Assert.Equal(ThreatLevel.SAFE, score.ThreatLevel);
    }

    [Fact]
    public void CalculateRisk_HoneyAccess_ElevatesScoreAboveBaseline()
    {
        var honey = new SecurityEvent
        {
            Type = EventType.HONEY_ACCESS,
            Source = EventSource.DECEPTION,
            Severity = EventSeverity.CRITICAL,
            IsHoney = true,
            Timestamp = DateTime.UtcNow,
            RiskContribution = 70,
            File = new FileContext { Path = @"C:\decoy\passwords.txt", SensitivityLevel = 9, IsHoney = true }
        };

        var score = _engine.CalculateRisk([honey]);

        Assert.True(score.Value > 50, $"Expected elevated risk, got {score.Value}");
        Assert.True(score.Confidence >= 0.8);
        Assert.True(score.ThreatLevel >= ThreatLevel.MEDIUM);
        Assert.Contains(score.Factors, f => f.Name == nameof(EventType.HONEY_ACCESS));
    }

    [Fact]
    public void DetermineThreatLevel_ConfidenceGate_CapsEscalation()
    {
        var gated = _engine.DetermineThreatLevel(riskScore: 150, confidence: 0.2);
        var ungated = _engine.DetermineThreatLevel(riskScore: 150, confidence: 0.9);

        Assert.Equal(ThreatLevel.LOW, gated);
        Assert.Equal(ThreatLevel.CRITICAL, ungated);
    }

    [Fact]
    public void ApplyDecay_ReducesValueOverTime()
    {
        var initial = new RiskScore
        {
            Value = 100,
            Confidence = 0.9,
            ThreatLevel = ThreatLevel.HIGH
        };

        var decayed = _engine.ApplyDecay(initial, TimeSpan.FromMinutes(30));

        Assert.True(decayed.Value < initial.Value);
        Assert.True(decayed.Confidence < initial.Confidence);
    }

    [Fact]
    public void CombineRisks_AveragesConfidenceAndSoftCaps()
    {
        var a = new RiskScore { Value = 80, Confidence = 0.8 };
        var b = new RiskScore { Value = 100, Confidence = 0.6 };

        var combined = _engine.CombineRisks([a, b]);

        Assert.Equal(0.7, combined.Confidence, precision: 3);
        Assert.True(combined.Value <= 200);
        Assert.True(combined.Value > 0);
    }
}
