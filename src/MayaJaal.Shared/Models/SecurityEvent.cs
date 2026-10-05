using System.Text.Json.Serialization;

namespace MayaJaal.Shared.Models;

/// <summary>
/// Core security event model for MayaJaal telemetry.
/// </summary>
public sealed class SecurityEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("sequence")]
    public long Sequence { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("eventType")]
    public EventType Type { get; set; }

    [JsonPropertyName("source")]
    public EventSource Source { get; set; }

    [JsonPropertyName("severity")]
    public EventSeverity Severity { get; set; }

    [JsonPropertyName("userContext")]
    public UserContext? User { get; set; }

    [JsonPropertyName("processContext")]
    public ProcessContext? Process { get; set; }

    [JsonPropertyName("fileContext")]
    public FileContext? File { get; set; }

    [JsonPropertyName("deviceContext")]
    public DeviceContext? Device { get; set; }

    [JsonPropertyName("riskContribution")]
    public int RiskContribution { get; set; }

    [JsonPropertyName("isHoney")]
    public bool IsHoney { get; set; }

    [JsonPropertyName("isProtected")]
    public bool IsProtected { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    [JsonPropertyName("hashChainPrev")]
    public string? HashChainPrevious { get; set; }

    [JsonPropertyName("hashChainCurrent")]
    public string? HashChainCurrent { get; set; }

    [JsonPropertyName("integrityHash")]
    public string? IntegrityHash { get; set; }

    /// <summary>Convenience path for UI bindings.</summary>
    [JsonIgnore]
    public string Path => File?.Path ?? Process?.ProcessName ?? Device?.DeviceName ?? Type.ToString();
}

public enum EventType
{
    FILE_ACCESS, FILE_MODIFY, FILE_DELETE, FILE_RENAME, FILE_COPY, FILE_MOVE,
    PROCESS_START, PROCESS_EXIT,
    USB_INSERT, USB_REMOVE, USB_FILE_ACCESS,
    HONEY_ACCESS, HONEY_MODIFY,
    VAULT_ACCESS, VAULT_LOCK, VAULT_UNLOCK,
    AUTH_SUCCESS, AUTH_FAILURE,
    MASS_FILE_ACTIVITY, RANSOMWARE_BEHAVIOR,
    POLICY_CHANGE, SECURITY_SERVICE_CHANGE,
    SYSTEM_EVENT, HEALTH_CHECK
}

public enum EventSource
{
    FILE_SYSTEM, PROCESS_MANAGER, USB_MANAGER,
    VAULT_MANAGER, AUTHENTICATION, POLICY_ENGINE,
    RISK_ENGINE, CORRELATION_ENGINE, SYSTEM, DECEPTION
}

public enum EventSeverity
{
    INFO = 0, LOW = 1, MEDIUM = 2, HIGH = 3, CRITICAL = 4
}

public sealed class UserContext
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Domain { get; set; }
    public string? SessionId { get; set; }
    public bool IsElevated { get; set; }
    public string? IpAddress { get; set; }
}

public sealed class ProcessContext
{
    public int ProcessId { get; set; }
    public int ParentProcessId { get; set; }
    public string? ProcessName { get; set; }
    public string? ProcessPath { get; set; }
    public string? CommandLine { get; set; }
    public DateTime StartTime { get; set; }
    public bool IsSuspicious { get; set; }
    public List<string> CommandLineArgs { get; set; } = new();
}

public sealed class FileContext
{
    public string? Path { get; set; }
    public string? Extension { get; set; }
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string? Hash { get; set; }
    public bool IsHoney { get; set; }
    public bool IsProtected { get; set; }
    public int SensitivityLevel { get; set; } = 5;
}

public sealed class DeviceContext
{
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceType { get; set; }
    public string? SerialNumber { get; set; }
    public string? VendorId { get; set; }
    public string? ProductId { get; set; }
    public long TotalSize { get; set; }
    public long FreeSize { get; set; }
    public bool IsEncrypted { get; set; }
}
