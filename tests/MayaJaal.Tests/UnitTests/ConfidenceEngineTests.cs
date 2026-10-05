using MayaJaal.Domain.Engines;
using MayaJaal.Shared.Models;
using Xunit;

namespace MayaJaal.Tests.UnitTests;

public class ConfidenceEngineTests
{
    private readonly ConfidenceEngine _engine = new();

    [Fact]
    public void CalculateConfidence_EmptyEvents_ReturnsZero()
    {
        var confidence = _engine.CalculateConfidence(Array.Empty<SecurityEvent>());
        Assert.Equal(0, confidence);
    }

    [Fact]
    public void CalculateConfidence_UsbHoneyMass_IsHigh()
    {
        var now = DateTime.UtcNow;
        var events = new[]
        {
            new SecurityEvent
            {
                Type = EventType.USB_INSERT,
                Source = EventSource.USB_MANAGER,
                Severity = EventSeverity.MEDIUM,
                Timestamp = now.AddMinutes(-2)
            },
            new SecurityEvent
            {
                Type = EventType.HONEY_ACCESS,
                Source = EventSource.DECEPTION,
                Severity = EventSeverity.HIGH,
                IsHoney = true,
                Timestamp = now.AddMinutes(-1)
            },
            new SecurityEvent
            {
                Type = EventType.MASS_FILE_ACTIVITY,
                Source = EventSource.FILE_SYSTEM,
                Severity = EventSeverity.CRITICAL,
                Timestamp = now,
                Process = new ProcessContext { ProcessId = 42, ProcessName = "copy.exe", IsSuspicious = true }
            }
        };

        var confidence = _engine.CalculateConfidence(events);
        var factors = _engine.GetConfidenceFactors(events);

        Assert.True(confidence >= 0.7, $"Expected high confidence, got {confidence}");
        Assert.True(factors.CorrelationQuality >= 0.9);
        Assert.True(factors.SignalStrength > 0.4);
    }

    [Fact]
    public void UpdateConfidence_BlendsPriorAndNew()
    {
        var newEvents = new[]
        {
            new SecurityEvent
            {
                Type = EventType.HONEY_ACCESS,
                IsHoney = true,
                Severity = EventSeverity.HIGH,
                Timestamp = DateTime.UtcNow
            }
        };

        var updated = _engine.UpdateConfidence(current: 0.2, newEvents: newEvents);
        var fresh = _engine.CalculateConfidence(newEvents);

        Assert.InRange(updated, 0, 1);
        Assert.True(Math.Abs(updated - (0.2 * 0.4 + fresh * 0.6)) < 0.001);
    }

    [Theory]
    [InlineData(ResponseAction.MONITOR, 0.0)]
    [InlineData(ResponseAction.ALERT, 0.5)]
    [InlineData(ResponseAction.LOCK_VAULT, 0.7)]
    [InlineData(ResponseAction.CONTAIN, 0.8)]
    [InlineData(ResponseAction.EMERGENCY_LOCKDOWN, 0.9)]
    public void GetConfidenceThreshold_MatchesActionPolicy(ResponseAction action, double expected)
    {
        Assert.Equal(expected, _engine.GetConfidenceThreshold(action));
    }
}
