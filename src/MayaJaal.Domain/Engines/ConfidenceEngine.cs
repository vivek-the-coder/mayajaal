using MayaJaal.Domain.Services;
using MayaJaal.Shared.Models;

namespace MayaJaal.Domain.Engines;

public sealed class ConfidenceEngine : IConfidenceEngine
{
    public double CalculateConfidence(IEnumerable<SecurityEvent> events)
    {
        var factors = GetConfidenceFactors(events);
        return Clamp(
            factors.SignalStrength * 0.4 +
            factors.CorrelationQuality * 0.3 +
            factors.ContextConsistency * 0.2 +
            factors.HistoricalAccuracy * 0.1);
    }

    public double UpdateConfidence(double current, IEnumerable<SecurityEvent> newEvents)
    {
        var next = CalculateConfidence(newEvents);
        return Clamp(current * 0.4 + next * 0.6);
    }

    public double GetConfidenceThreshold(ResponseAction action) => action switch
    {
        ResponseAction.NONE or ResponseAction.MONITOR => 0.0,
        ResponseAction.ALERT or ResponseAction.NOTIFY_USER => 0.5,
        ResponseAction.LOCK_VAULT or ResponseAction.RESTRICT_ASSETS or ResponseAction.PRESERVE_EVIDENCE => 0.7,
        ResponseAction.CONTAIN => 0.8,
        ResponseAction.EMERGENCY_LOCKDOWN => 0.9,
        _ => 0.6
    };

    public ConfidenceFactors GetConfidenceFactors(IEnumerable<SecurityEvent> events)
    {
        var list = events.ToList();
        if (list.Count == 0)
            return new ConfidenceFactors();

        var highValue = list.Count(e =>
            e.IsHoney ||
            e.Type is EventType.HONEY_ACCESS or EventType.HONEY_MODIFY or
                EventType.MASS_FILE_ACTIVITY or EventType.RANSOMWARE_BEHAVIOR);
        var signalStrength = Clamp((double)highValue / Math.Max(1, list.Count) + (list.Any(e => e.IsHoney) ? 0.4 : 0));

        var hasUsb = list.Any(e => e.Type is EventType.USB_INSERT or EventType.USB_FILE_ACCESS);
        var hasHoney = list.Any(e => e.IsHoney || e.Type is EventType.HONEY_ACCESS or EventType.HONEY_MODIFY);
        var hasMass = list.Any(e => e.Type is EventType.MASS_FILE_ACTIVITY or EventType.FILE_COPY);
        var hasProcess = list.Any(e => e.Process?.IsSuspicious == true || e.Type == EventType.PROCESS_START);

        double correlationQuality = 0.2;
        if (hasHoney) correlationQuality = 0.3;
        if (hasUsb && hasHoney) correlationQuality = 0.6;
        if (hasUsb && hasHoney && hasMass) correlationQuality = 0.9;
        if (hasUsb && hasHoney && hasMass && hasProcess) correlationQuality = 0.95;

        var span = list.Max(e => e.Timestamp) - list.Min(e => e.Timestamp);
        var contextConsistency = span.TotalMinutes <= 10 ? 0.85 : span.TotalMinutes <= 60 ? 0.65 : 0.4;
        var historicalAccuracy = 0.75; // bootstrap prior until feedback loop exists
        var temporalRelevance = list.Any(e => (DateTime.UtcNow - e.Timestamp).TotalMinutes < 5) ? 0.9 : 0.5;

        return new ConfidenceFactors
        {
            SignalStrength = signalStrength,
            CorrelationQuality = correlationQuality,
            ContextConsistency = contextConsistency,
            HistoricalAccuracy = historicalAccuracy,
            TemporalRelevance = temporalRelevance
        };
    }

    private static double Clamp(double v) => Math.Max(0, Math.Min(1, v));
}
