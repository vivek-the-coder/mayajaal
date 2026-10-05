namespace MayaJaal.Shared.Models;

public sealed class Vault
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public VaultState State { get; set; } = VaultState.LOCKED;
    public bool IsEncrypted { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LockedAt { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public string Path { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public int ItemCount { get; set; }
    public EncryptionParameters Encryption { get; set; } = new();
    public RecoveryMetadata Recovery { get; set; } = new();
    public List<VaultItem> Items { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum VaultState
{
    LOCKED,
    UNLOCKING,
    UNLOCKED,
    LOCKING,
    RECOVERING,
    ERROR
}

public sealed class EncryptionParameters
{
    public string Algorithm { get; set; } = "AES-256-GCM";
    public int KeySize { get; set; } = 256;
    public int IvSize { get; set; } = 12;
    public string? Salt { get; set; }
    public string? WrappedKey { get; set; }
    public string KdfAlgorithm { get; set; } = "Argon2id";
    public int KdfMemory { get; set; } = 65536;
    public int KdfIterations { get; set; } = 3;
    public int KdfParallelism { get; set; } = 1;
    public string? IntegrityHash { get; set; }
}

public sealed class RecoveryMetadata
{
    public bool HasRecoveryKey { get; set; }
    public string? WrappedRecoveryKey { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int AttemptsRemaining { get; set; } = 5;
    public DateTime? LastAttemptAt { get; set; }
    public List<RecoveryMethod> Methods { get; set; } = new();
}

public enum RecoveryMethod
{
    RECOVERY_KEY,
    BACKUP_FILE,
    ESCROWED_KEY
}

public sealed class VaultItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? OriginalPath { get; set; }
    public string? EncryptedPath { get; set; }
    public long Size { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
    public DateTime? AccessedAt { get; set; }
    public string? Hash { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public bool IsDirectory { get; set; }
    public List<VaultItem> Children { get; set; } = new();
}
