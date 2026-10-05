using System.Text.Json.Serialization;
using MayaJaal.Shared.Models;

namespace MayaJaal.Shared.IPC;

public static class IpcCommands
{
    public const string GetStatus = "GET_STATUS";
    public const string GetIncident = "GET_INCIDENT";
    public const string GetEvents = "GET_EVENTS";
    public const string GetAssets = "GET_ASSETS";
    public const string LockVault = "LOCK_VAULT";
    public const string Ping = "PING";
    public const string GetVaults = "GET_VAULTS";
    public const string GetIncidents = "GET_INCIDENTS";
    public const string ResolveIncident = "RESOLVE_INCIDENT";
    public const string GetPolicies = "GET_POLICIES";
    public const string ExecuteRollback = "EXECUTE_ROLLBACK";
    public const string RunAttackSimulation = "RUN_ATTACK_SIMULATION";
    public const string AddVaultItem = "ADD_VAULT_ITEM";
    public const string UnlockVault = "UNLOCK_VAULT";
}

public sealed class IpcRequest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("vaultId")]
    public string? VaultId { get; set; }

    [JsonPropertyName("incidentId")]
    public string? IncidentId { get; set; }

    [JsonPropertyName("resolvedBy")]
    public string? ResolvedBy { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; } = 50;

    [JsonPropertyName("scenario")]
    public string? Scenario { get; set; }

    [JsonPropertyName("sourcePath")]
    public string? SourcePath { get; set; }

    [JsonPropertyName("payload")]
    public Dictionary<string, object>? Payload { get; set; }
}

public sealed class IpcResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("status")]
    public ThreatState? Status { get; set; }

    [JsonPropertyName("incident")]
    public Incident? Incident { get; set; }

    [JsonPropertyName("incidents")]
    public List<Incident>? Incidents { get; set; }

    [JsonPropertyName("events")]
    public List<SecurityEvent>? Events { get; set; }

    [JsonPropertyName("assets")]
    public List<ProtectedAsset>? Assets { get; set; }

    [JsonPropertyName("vaults")]
    public List<VaultSummary>? Vaults { get; set; }

    [JsonPropertyName("policies")]
    public List<PolicySummary>? Policies { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public sealed class VaultSummary
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("itemCount")]
    public int ItemCount { get; set; }

    [JsonPropertyName("totalSize")]
    public long TotalSize { get; set; }

    [JsonPropertyName("ownerId")]
    public string OwnerId { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<VaultItemSummary> Items { get; set; } = [];
}

public sealed class VaultItemSummary
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("originalPath")]
    public string? OriginalPath { get; set; }

    [JsonPropertyName("addedAt")]
    public DateTime AddedAt { get; set; }
}

public sealed class PolicySummary
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("defaultAction")]
    public string DefaultAction { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";
}

public static class IpcDefaults
{
    public const string PipeName = "MayaJaal.Guardian";
    public const int MaxMessageBytes = 16 * 1024 * 1024;
}
