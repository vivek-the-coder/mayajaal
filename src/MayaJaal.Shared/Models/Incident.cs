using System.Text.Json.Serialization;

namespace MayaJaal.Shared.Models;

public sealed class Incident
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; set; }

    [JsonPropertyName("threatLevel")]
    public ThreatLevel Level { get; set; }

    [JsonPropertyName("riskScore")]
    public int RiskScore { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("status")]
    public IncidentStatus Status { get; set; } = IncidentStatus.OPEN;

    [JsonPropertyName("primarySignal")]
    public string PrimarySignal { get; set; } = string.Empty;

    [JsonPropertyName("responseAction")]
    public ResponseAction Action { get; set; }

    [JsonPropertyName("events")]
    public List<SecurityEvent> Events { get; set; } = new();

    [JsonPropertyName("evidence")]
    public List<Evidence> Evidence { get; set; } = new();

    [JsonPropertyName("timeline")]
    public List<TimelineEntry> Timeline { get; set; } = new();

    [JsonPropertyName("affectedAssets")]
    public List<ProtectedAsset> AffectedAssets { get; set; } = new();

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("resolvedBy")]
    public string? ResolvedBy { get; set; }

    [JsonPropertyName("resolutionNotes")]
    public string? ResolutionNotes { get; set; }
}

public enum ThreatLevel
{
    SAFE = 0,
    LOW = 1,
    MEDIUM = 2,
    HIGH = 3,
    CRITICAL = 4
}

public enum IncidentStatus
{
    OPEN,
    UNDER_ANALYSIS,
    CONTAINMENT_PENDING,
    CONTAINED,
    RESOLVED,
    ARCHIVED,
    CLOSED
}

public enum ResponseAction
{
    NONE,
    MONITOR,
    ALERT,
    LOCK_VAULT,
    RESTRICT_ASSETS,
    CONTAIN,
    EMERGENCY_LOCKDOWN,
    NOTIFY_USER,
    PRESERVE_EVIDENCE,
    GENERATE_REPORT
}

public sealed class TimelineEntry
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int RiskDelta { get; set; }
    public string? Actor { get; set; }
    public string? Target { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

public sealed class Evidence
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public EvidenceType Type { get; set; }
    public string? Path { get; set; }
    public string? Hash { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    public IntegrityStatus Status { get; set; } = IntegrityStatus.VERIFIED;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum EvidenceType
{
    EVENT_LOG,
    FILE_COPY,
    SCREENSHOT,
    PROCESS_DUMP,
    USB_INFO,
    NETWORK_LOG,
    SYSTEM_INFO,
    REGISTRY_SNAPSHOT
}

public enum IntegrityStatus
{
    VERIFIED,
    TAMPERED,
    UNVERIFIED,
    PARTIAL
}
