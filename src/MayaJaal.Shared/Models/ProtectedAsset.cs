namespace MayaJaal.Shared.Models;

public sealed class ProtectedAsset
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Path { get; set; } = string.Empty;
    public AssetType Type { get; set; }
    public AssetState State { get; set; } = AssetState.PROTECTED;
    public string OwnerId { get; set; } = string.Empty;
    public DateTime ProtectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModified { get; set; }
    public DateTime? LastAccessed { get; set; }
    public string? IntegrityHash { get; set; }
    public int SensitivityLevel { get; set; } = 5;
    public bool IsHoney { get; set; }
    public bool IsEncrypted { get; set; }
    public string? PolicyId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public List<AssetAccess> AccessHistory { get; set; } = new();
}

public enum AssetType
{
    DOCUMENT,
    DESKTOP,
    DOWNLOADS,
    PICTURES,
    VIDEOS,
    CUSTOM
}

public enum AssetState
{
    PROTECTED,
    UNPROTECTED,
    RESTRICTED,
    QUARANTINED,
    UNDER_INVESTIGATION
}

public sealed class AssetAccess
{
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
    public string? ProcessName { get; set; }
    public int ProcessId { get; set; }
    public AccessType Type { get; set; }
    public bool IsAuthorized { get; set; }
    public string? Details { get; set; }
}

public enum AccessType
{
    READ,
    WRITE,
    DELETE,
    COPY,
    MOVE,
    RENAME,
    EXECUTE
}

public sealed class HoneyFile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DecoyType DecoyType { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
    public string? PolicyId { get; set; }
    public int Sensitivity { get; set; } = 8;
    public HoneyStatus Status { get; set; } = HoneyStatus.Active;
    public DateTime? LastRotation { get; set; }
    public string? ContentHash { get; set; }
    public string TemplateVersion { get; set; } = "1.0";
    public List<AssetAccess> AccessHistory { get; set; } = new();
}

public enum DecoyType
{
    Financial,
    Credential,
    Strategy,
    HR,
    API,
    Personal
}

public enum HoneyStatus
{
    Active,
    Rotated,
    Compromised,
    Disabled
}

public sealed class ThreatState
{
    public int CurrentRisk { get; set; }
    public double Confidence { get; set; }
    public ThreatLevel Level { get; set; }
    public List<SecurityEvent> RecentEvents { get; set; } = new();
    public Incident? ActiveIncident { get; set; }
    public bool GuardianOnline { get; set; } = true;
    public int EventCount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
