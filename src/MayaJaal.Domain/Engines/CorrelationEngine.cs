using MayaJaal.Domain.Services;
using MayaJaal.Shared.Models;

namespace MayaJaal.Domain.Engines;

public sealed class CorrelationEngine : ICorrelationEngine
{
    private static readonly List<ThreatPattern> Patterns =
    [
        new()
        {
            Name = "USB_Honey",
            Description = "Removable device with decoy access",
            EventSequence = ["USB_INSERT", "HONEY_ACCESS"],
            SeverityWeight = 50,
            ConfidenceMultiplier = 1.5,
            TypicalRiskScore = 70
        },
        new()
        {
            Name = "Honey_MassCopy",
            Description = "Decoy access followed by bulk copy",
            EventSequence = ["HONEY_ACCESS", "MASS_FILE_ACTIVITY"],
            SeverityWeight = 70,
            ConfidenceMultiplier = 2.0,
            TypicalRiskScore = 100
        },
        new()
        {
            Name = "USB_Process_MassCopy",
            Description = "Full exfiltration pattern",
            EventSequence = ["USB_INSERT", "PROCESS_START", "MASS_FILE_ACTIVITY"],
            SeverityWeight = 85,
            ConfidenceMultiplier = 2.5,
            TypicalRiskScore = 130
        },
        new()
        {
            Name = "Insider_Credential_Exfil",
            Description = "Insider threat: credential decoy + mass copy",
            EventSequence = ["HONEY_ACCESS", "FILE_COPY", "MASS_FILE_ACTIVITY"],
            SeverityWeight = 95,
            ConfidenceMultiplier = 3.0,
            TypicalRiskScore = 150
        },
        new()
        {
            Name = "Ransomware_Like",
            Description = "Mass encryption / rename behavior",
            EventSequence = ["MASS_FILE_ACTIVITY", "RANSOMWARE_BEHAVIOR"],
            SeverityWeight = 100,
            ConfidenceMultiplier = 2.8,
            TypicalRiskScore = 160
        }
    ];

    public CorrelationResult CorrelateEvents(IEnumerable<SecurityEvent> events)
    {
        var list = events.OrderBy(e => e.Timestamp).ToList();
        var detected = DetectPatterns(list);
        if (detected.Count == 0)
        {
            return new CorrelationResult
            {
                IsCorrelated = list.Count >= 3,
                CorrelationStrength = GetCorrelationScore(list),
                CorrelationChain = list.Select(e => e.Type.ToString()).Distinct().ToList(),
                PatternType = "Generic"
            };
        }

        var best = detected.OrderByDescending(p => p.SeverityWeight).First();
        return new CorrelationResult
        {
            IsCorrelated = true,
            CorrelationStrength = Math.Min(1.0, best.ConfidenceMultiplier / 3.0),
            CorrelationChain = best.EventSequence,
            PatternType = best.Name,
            Evidence = new Dictionary<string, object>
            {
                ["description"] = best.Description,
                ["typicalRisk"] = best.TypicalRiskScore,
                ["matchedEvents"] = list.Count
            }
        };
    }

    public List<ThreatPattern> DetectPatterns(IEnumerable<SecurityEvent> events)
    {
        var types = events.Select(e => Normalize(e)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Patterns.Where(p => p.EventSequence.All(s => types.Contains(s))).ToList();
    }

    public double GetCorrelationScore(IEnumerable<SecurityEvent> events)
    {
        var list = events.ToList();
        if (list.Count < 2) return 0;
        var relatedPairs = 0;
        var total = 0;
        for (var i = 0; i < list.Count; i++)
        {
            for (var j = i + 1; j < list.Count; j++)
            {
                total++;
                if (IsEventRelated(list[i], list[j])) relatedPairs++;
            }
        }
        return total == 0 ? 0 : (double)relatedPairs / total;
    }

    public bool IsEventRelated(SecurityEvent event1, SecurityEvent event2)
    {
        var sameUser = !string.IsNullOrEmpty(event1.User?.UserId) &&
                       event1.User?.UserId == event2.User?.UserId;
        var closeInTime = Math.Abs((event1.Timestamp - event2.Timestamp).TotalMinutes) <= 15;
        var sameProcess = event1.Process?.ProcessId > 0 &&
                          event1.Process?.ProcessId == event2.Process?.ProcessId;
        var usbChain = (event1.Type == EventType.USB_INSERT && event2.IsHoney) ||
                       (event2.Type == EventType.USB_INSERT && event1.IsHoney);
        return (sameUser && closeInTime) || sameProcess || usbChain;
    }

    private static string Normalize(SecurityEvent e)
    {
        if (e.IsHoney && e.Type is not (EventType.HONEY_ACCESS or EventType.HONEY_MODIFY))
            return EventType.HONEY_ACCESS.ToString();
        return e.Type.ToString();
    }
}
