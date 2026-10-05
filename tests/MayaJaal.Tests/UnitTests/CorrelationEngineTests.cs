using MayaJaal.Domain.Engines;
using MayaJaal.Shared.Models;
using Xunit;

namespace MayaJaal.Tests.UnitTests;

public class CorrelationEngineTests
{
    private readonly CorrelationEngine _engine = new();

    [Fact]
    public void DetectPatterns_UsbHoney_MatchesUsbHoneyPattern()
    {
        var events = new[]
        {
            new SecurityEvent { Type = EventType.USB_INSERT, Timestamp = DateTime.UtcNow.AddMinutes(-3) },
            new SecurityEvent { Type = EventType.HONEY_ACCESS, IsHoney = true, Timestamp = DateTime.UtcNow }
        };

        var patterns = _engine.DetectPatterns(events);

        Assert.Contains(patterns, p => p.Name == "USB_Honey");
    }

    [Fact]
    public void CorrelateEvents_FullExfilPattern_ReturnsStrongCorrelation()
    {
        var events = new[]
        {
            new SecurityEvent { Type = EventType.USB_INSERT, Timestamp = DateTime.UtcNow.AddMinutes(-5) },
            new SecurityEvent { Type = EventType.PROCESS_START, Timestamp = DateTime.UtcNow.AddMinutes(-3) },
            new SecurityEvent { Type = EventType.MASS_FILE_ACTIVITY, Timestamp = DateTime.UtcNow }
        };

        var result = _engine.CorrelateEvents(events);

        Assert.True(result.IsCorrelated);
        Assert.Equal("USB_Process_MassCopy", result.PatternType);
        Assert.True(result.CorrelationStrength > 0.5);
        Assert.Contains("USB_INSERT", result.CorrelationChain);
    }

    [Fact]
    public void IsEventRelated_SameUserWithinWindow_ReturnsTrue()
    {
        var a = new SecurityEvent
        {
            Type = EventType.FILE_ACCESS,
            Timestamp = DateTime.UtcNow,
            User = new UserContext { UserId = "u1" }
        };
        var b = new SecurityEvent
        {
            Type = EventType.FILE_COPY,
            Timestamp = DateTime.UtcNow.AddMinutes(5),
            User = new UserContext { UserId = "u1" }
        };

        Assert.True(_engine.IsEventRelated(a, b));
    }

    [Fact]
    public void IsEventRelated_UsbThenHoney_ReturnsTrue()
    {
        var usb = new SecurityEvent { Type = EventType.USB_INSERT, Timestamp = DateTime.UtcNow };
        var honey = new SecurityEvent
        {
            Type = EventType.FILE_ACCESS,
            IsHoney = true,
            Timestamp = DateTime.UtcNow.AddMinutes(1)
        };

        Assert.True(_engine.IsEventRelated(usb, honey));
    }

    [Fact]
    public void GetCorrelationScore_UnrelatedEvents_IsLow()
    {
        var events = new[]
        {
            new SecurityEvent
            {
                Type = EventType.HEALTH_CHECK,
                Timestamp = DateTime.UtcNow.AddHours(-2),
                User = new UserContext { UserId = "a" }
            },
            new SecurityEvent
            {
                Type = EventType.SYSTEM_EVENT,
                Timestamp = DateTime.UtcNow,
                User = new UserContext { UserId = "b" }
            }
        };

        var score = _engine.GetCorrelationScore(events);
        Assert.Equal(0, score);
    }

    [Fact]
    public void DetectPatterns_HoneyNormalizedFromIsHoneyFlag()
    {
        // IsHoney on non-HONEY_* types normalizes to HONEY_ACCESS for pattern matching
        var events = new[]
        {
            new SecurityEvent { Type = EventType.USB_INSERT, Timestamp = DateTime.UtcNow.AddMinutes(-1) },
            new SecurityEvent
            {
                Type = EventType.FILE_ACCESS,
                IsHoney = true,
                Timestamp = DateTime.UtcNow
            }
        };

        var patterns = _engine.DetectPatterns(events);
        Assert.Contains(patterns, p => p.Name == "USB_Honey");
    }
}
