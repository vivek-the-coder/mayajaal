using MayaJaal.Domain.Services;
using MayaJaal.Shared.Models;

namespace MayaJaal.Domain.Engines;

/// <summary>
/// Risk scoring with time decay: R(t) = Σ[Wᵢ × Cᵢ × Aᵢ × Xᵢ × e^(-λᵢΔtᵢ)]
/// </summary>
public sealed class RiskEngine : IRiskEngine
{
    private static readonly Dictionary<EventType, (int Min, int Max, double Decay)> SignalWeights = new()
    {
        [EventType.FILE_ACCESS] = (1, 5, 0.05),
        [EventType.FILE_MODIFY] = (5, 15, 0.04),
        [EventType.FILE_COPY] = (10, 25, 0.03),
        [EventType.FILE_MOVE] = (10, 25, 0.03),
        [EventType.FILE_DELETE] = (15, 30, 0.02),
        [EventType.MASS_FILE_ACTIVITY] = (30, 50, 0.01),
        [EventType.PROCESS_START] = (5, 20, 0.04),
        [EventType.USB_INSERT] = (5, 15, 0.03),
        [EventType.USB_FILE_ACCESS] = (15, 35, 0.02),
        [EventType.HONEY_ACCESS] = (50, 80, 0.005),
        [EventType.HONEY_MODIFY] = (60, 90, 0.005),
        [EventType.RANSOMWARE_BEHAVIOR] = (80, 100, 0.001),
        [EventType.AUTH_FAILURE] = (10, 25, 0.04),
        [EventType.VAULT_ACCESS] = (5, 20, 0.03),
    };

    public RiskScore CalculateRisk(IEnumerable<SecurityEvent> events)
    {
        var list = events.ToList();
        if (list.Count == 0)
        {
            return new RiskScore { Value = 0, Confidence = 0, ThreatLevel = ThreatLevel.SAFE };
        }

        var now = DateTime.UtcNow;
        var factors = new List<RiskFactor>();
        double total = 0;

        foreach (var evt in list)
        {
            var (min, max, decay) = SignalWeights.GetValueOrDefault(evt.Type, (1, 10, 0.05));
            var baseWeight = evt.RiskContribution > 0
                ? evt.RiskContribution
                : (min + max) / 2;

            if (evt.IsHoney)
                baseWeight = Math.Max(baseWeight, 55);

            var sensitivity = evt.File?.SensitivityLevel ?? 5;
            var contextMultiplier = GetContextMultiplier(evt);
            var confidence = evt.IsHoney ? 0.9 : evt.Severity >= EventSeverity.HIGH ? 0.8 : 0.6;
            var elapsedMinutes = Math.Max(0, (now - evt.Timestamp).TotalMinutes);
            var decayFactor = Math.Exp(-decay * elapsedMinutes);
            var contribution = baseWeight * confidence * (sensitivity / 5.0) * contextMultiplier * decayFactor;

            total += contribution;
            factors.Add(new RiskFactor
            {
                Name = evt.Type.ToString(),
                Weight = baseWeight,
                Confidence = confidence,
                Source = evt.Source.ToString(),
                Contribution = (int)Math.Round(contribution),
                Details = new Dictionary<string, object>
                {
                    ["path"] = evt.Path,
                    ["decay"] = decayFactor,
                    ["elapsedMinutes"] = elapsedMinutes
                }
            });
        }

        // Soft cap so correlated bursts can exceed 100 into CRITICAL range mapping
        var value = (int)Math.Min(200, Math.Round(total));
        var score = new RiskScore
        {
            Value = value,
            Confidence = Math.Min(1.0, factors.Average(f => f.Confidence)),
            CalculatedAt = now,
            Factors = factors,
            ThreatLevel = DetermineThreatLevel(value, factors.Average(f => f.Confidence))
        };
        return score;
    }

    public RiskScore ApplyDecay(RiskScore risk, TimeSpan elapsed)
    {
        var decay = Math.Exp(-0.02 * elapsed.TotalMinutes);
        var value = (int)Math.Round(risk.Value * decay);
        return new RiskScore
        {
            Value = value,
            Confidence = risk.Confidence * decay,
            CalculatedAt = DateTime.UtcNow,
            Factors = risk.Factors,
            ThreatLevel = DetermineThreatLevel(value, risk.Confidence * decay),
            Context = risk.Context
        };
    }

    public RiskScore CombineRisks(IEnumerable<RiskScore> risks)
    {
        var list = risks.ToList();
        if (list.Count == 0)
            return new RiskScore();

        var value = (int)Math.Min(200, list.Sum(r => r.Value) * 0.7);
        var confidence = list.Average(r => r.Confidence);
        return new RiskScore
        {
            Value = value,
            Confidence = confidence,
            ThreatLevel = DetermineThreatLevel(value, confidence),
            Factors = list.SelectMany(r => r.Factors).ToList()
        };
    }

    public ThreatLevel DetermineThreatLevel(int riskScore, double confidence)
    {
        // Confidence-gated escalation: low confidence caps at MEDIUM
        if (confidence < 0.35 && riskScore > 50)
            riskScore = Math.Min(riskScore, 50);

        return riskScore switch
        {
            <= 20 => ThreatLevel.SAFE,
            <= 50 => ThreatLevel.LOW,
            <= 80 => ThreatLevel.MEDIUM,
            <= 120 => ThreatLevel.HIGH,
            _ => ThreatLevel.CRITICAL
        };
    }

    public List<RiskFactor> GetRiskFactors(IEnumerable<SecurityEvent> events)
        => CalculateRisk(events).Factors;

    private static double GetContextMultiplier(SecurityEvent evt)
    {
        if (evt.IsHoney && evt.Type is EventType.HONEY_ACCESS or EventType.HONEY_MODIFY)
            return 1.5;
        if (evt.Process?.IsSuspicious == true)
            return 1.4;
        if (evt.Device?.DeviceType?.Equals("USB", StringComparison.OrdinalIgnoreCase) == true)
            return 1.2;
        if (evt.IsProtected)
            return 1.15;
        return 1.0;
    }
}
