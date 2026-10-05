using MayaJaal.Shared.Models;

namespace MayaJaal.Domain.Services;

public interface IRiskEngine
{
    RiskScore CalculateRisk(IEnumerable<SecurityEvent> events);
    RiskScore ApplyDecay(RiskScore risk, TimeSpan elapsed);
    RiskScore CombineRisks(IEnumerable<RiskScore> risks);
    ThreatLevel DetermineThreatLevel(int riskScore, double confidence);
    List<RiskFactor> GetRiskFactors(IEnumerable<SecurityEvent> events);
}

public sealed class RiskScore
{
    public int Value { get; set; }
    public double Confidence { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public ThreatLevel ThreatLevel { get; set; }
    public List<RiskFactor> Factors { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
}

public sealed class RiskFactor
{
    public string Name { get; set; } = string.Empty;
    public int Weight { get; set; }
    public double Confidence { get; set; }
    public string Source { get; set; } = string.Empty;
    public int Contribution { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

public interface IConfidenceEngine
{
    double CalculateConfidence(IEnumerable<SecurityEvent> events);
    double UpdateConfidence(double current, IEnumerable<SecurityEvent> newEvents);
    double GetConfidenceThreshold(ResponseAction action);
    ConfidenceFactors GetConfidenceFactors(IEnumerable<SecurityEvent> events);
}

public sealed class ConfidenceFactors
{
    public double SignalStrength { get; set; }
    public double CorrelationQuality { get; set; }
    public double ContextConsistency { get; set; }
    public double HistoricalAccuracy { get; set; }
    public double TemporalRelevance { get; set; }
}

public interface ICorrelationEngine
{
    CorrelationResult CorrelateEvents(IEnumerable<SecurityEvent> events);
    List<ThreatPattern> DetectPatterns(IEnumerable<SecurityEvent> events);
    double GetCorrelationScore(IEnumerable<SecurityEvent> events);
    bool IsEventRelated(SecurityEvent event1, SecurityEvent event2);
}

public sealed class CorrelationResult
{
    public bool IsCorrelated { get; set; }
    public double CorrelationStrength { get; set; }
    public List<string> CorrelationChain { get; set; } = new();
    public string? PatternType { get; set; }
    public Dictionary<string, object> Evidence { get; set; } = new();
}

public sealed class ThreatPattern
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> EventSequence { get; set; } = new();
    public int SeverityWeight { get; set; }
    public double ConfidenceMultiplier { get; set; } = 1.0;
    public int TypicalRiskScore { get; set; }
}
