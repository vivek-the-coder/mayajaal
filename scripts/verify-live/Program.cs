using MayaJaal.Shared.IPC;
using MayaJaal.Shared.Models;

var failures = new List<string>();
var passes = 0;

void Pass(string name)
{
    passes++;
    Console.WriteLine($"  PASS  {name}");
}

void Fail(string name, string detail)
{
    failures.Add($"{name}: {detail}");
    Console.WriteLine($"  FAIL  {name} — {detail}");
}

var sample = Path.Combine(Path.GetTempPath(), "mayajaal-fulltest.txt");
await File.WriteAllTextAsync(sample, "MayaJaal full test " + DateTime.UtcNow.ToString("o"));

await using var client = new GuardianClient(connectTimeout: TimeSpan.FromSeconds(8));

Console.WriteLine("== IPC FULL SUITE ==");

try
{
    await client.ConnectAsync();
    if (client.IsConnected) Pass("Connect");
    else Fail("Connect", "not connected");
}
catch (Exception ex)
{
    Fail("Connect", ex.Message);
    Console.WriteLine($"SUMMARY fails={failures.Count} passes={passes}");
    Environment.Exit(1);
}

async Task<IpcResponse?> Safe(string name, Func<Task<IpcResponse>> action)
{
    try
    {
        return await action();
    }
    catch (Exception ex)
    {
        Fail(name, ex.Message);
        return null;
    }
}

// GET_STATUS
{
    var r = await Safe("GET_STATUS", () => client.GetStatusAsync());
    if (r is null) { /* failed */ }
    else if (!r.Success || r.Status is null) Fail("GET_STATUS", r.Error ?? "null status");
    else Pass($"GET_STATUS (level={r.Status.Level}, risk={r.Status.CurrentRisk})");
}

// GET_EVENTS
{
    var r = await Safe("GET_EVENTS", () => client.GetEventsAsync(50));
    if (r is null) { }
    else if (!r.Success) Fail("GET_EVENTS", r.Error ?? "unsuccessful");
    else Pass($"GET_EVENTS (count={r.Events?.Count ?? 0})");
}

// GET_ASSETS
{
    var r = await Safe("GET_ASSETS", () => client.GetAssetsAsync());
    if (r is null) { }
    else if (!r.Success) Fail("GET_ASSETS", r.Error ?? "unsuccessful");
    else Pass($"GET_ASSETS (count={r.Assets?.Count ?? 0})");
}

// GET_POLICIES
{
    var r = await Safe("GET_POLICIES", () => client.GetPoliciesAsync());
    if (r is null) { }
    else if (!r.Success || r.Policies is null || r.Policies.Count == 0)
        Fail("GET_POLICIES", r.Error ?? "empty");
    else Pass($"GET_POLICIES (count={r.Policies.Count}, first={r.Policies[0].Name})");
}

// GET_INCIDENTS
{
    var r = await Safe("GET_INCIDENTS", () => client.GetIncidentsAsync(50));
    if (r is null) { }
    else if (!r.Success) Fail("GET_INCIDENTS", r.Error ?? "unsuccessful");
    else Pass($"GET_INCIDENTS (count={r.Incidents?.Count ?? 0})");
}

// GET_INCIDENT
{
    var r = await Safe("GET_INCIDENT", () => client.GetIncidentAsync());
    if (r is null) { }
    else if (!r.Success) Fail("GET_INCIDENT", r.Error ?? "unsuccessful");
    else Pass($"GET_INCIDENT (active={r.Incident?.Number ?? "none"})");
}

// UNLOCK_VAULT
{
    var r = await Safe("UNLOCK_VAULT", () => client.UnlockVaultAsync());
    if (r is null) { }
    else if (!r.Success) Fail("UNLOCK_VAULT", r.Error ?? r.Message ?? "failed");
    else Pass($"UNLOCK_VAULT ({r.Message})");
}

// ADD_VAULT_ITEM
{
    var r = await Safe("ADD_VAULT_ITEM", () => client.AddVaultItemAsync(null, sample));
    if (r is null) { }
    else if (!r.Success) Fail("ADD_VAULT_ITEM", r.Error ?? r.Message ?? "failed");
    else Pass($"ADD_VAULT_ITEM ({r.Message})");
}

// GET_VAULTS (verify item)
string? vaultId = null;
string? activeIncidentId = null;
{
    var r = await Safe("GET_VAULTS", () => client.GetVaultsAsync());
    if (r is null) { }
    else if (!r.Success || r.Vaults is null || r.Vaults.Count == 0)
        Fail("GET_VAULTS", r.Error ?? "empty");
    else
    {
        var v = r.Vaults[0];
        vaultId = v.Id;
        var hit = v.Items.Any(i => string.Equals(i.Name, Path.GetFileName(sample), StringComparison.OrdinalIgnoreCase));
        if (v.ItemCount < 1 || !hit)
            Fail("GET_VAULTS", $"item missing (items={v.ItemCount}, listed={v.Items.Count}, state={v.State})");
        else
            Pass($"GET_VAULTS (name={v.Name}, state={v.State}, items={v.ItemCount})");
    }
}

// RUN_ATTACK_SIMULATION usb-exfil
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    var r = await Safe("SIM_USB", () => client.RunAttackSimulationAsync("usb-exfil", cts.Token));
    if (r is null) { }
    else if (!r.Success) Fail("SIM_USB", r.Error ?? r.Message ?? "failed");
    else
    {
        Pass($"SIM_USB ({Truncate(r.Message, 100)})");
        activeIncidentId = r.Incident?.Id ?? r.Status?.ActiveIncident?.Id;
    }
}

// Status after sim
{
    var r = await Safe("STATUS_AFTER_USB", () => client.GetStatusAsync());
    if (r is null) { }
    else if (!r.Success || r.Status is null) Fail("STATUS_AFTER_USB", r.Error ?? "null");
    else if (r.Status.Level < ThreatLevel.HIGH)
        Fail("STATUS_AFTER_USB", $"expected HIGH+ got {r.Status.Level}");
    else
    {
        activeIncidentId ??= r.Incident?.Id ?? r.Status.ActiveIncident?.Id;
        Pass($"STATUS_AFTER_USB (level={r.Status.Level}, risk={r.Status.CurrentRisk}, incident={r.Incident?.Number ?? r.Status.ActiveIncident?.Number ?? "none"})");
    }
}

// LOCK_VAULT
{
    var r = await Safe("LOCK_VAULT", () => client.LockVaultAsync(vaultId));
    if (r is null) { }
    else if (!r.Success) Fail("LOCK_VAULT", r.Error ?? r.Message ?? "failed");
    else Pass($"LOCK_VAULT ({r.Message})");
}

// Unlock again for resolve/rollback path
{
    var r = await Safe("UNLOCK_AGAIN", () => client.UnlockVaultAsync(vaultId));
    if (r is null) { }
    else if (!r.Success) Fail("UNLOCK_AGAIN", r.Error ?? r.Message ?? "failed");
    else Pass("UNLOCK_AGAIN");
}

// RESOLVE_INCIDENT
if (!string.IsNullOrWhiteSpace(activeIncidentId))
{
    var r = await Safe("RESOLVE_INCIDENT", () => client.ResolveIncidentAsync(activeIncidentId, Environment.UserName, "full-test resolve"));
    if (r is null) { }
    else if (!r.Success) Fail("RESOLVE_INCIDENT", r.Error ?? r.Message ?? "failed");
    else Pass($"RESOLVE_INCIDENT ({activeIncidentId})");
}
else
{
    Fail("RESOLVE_INCIDENT", "no active incident id after USB sim");
}

// Insider scenario
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    var r = await Safe("SIM_INSIDER", () => client.RunAttackSimulationAsync("insider", cts.Token));
    if (r is null) { }
    else if (!r.Success) Fail("SIM_INSIDER", r.Error ?? r.Message ?? "failed");
    else
    {
        Pass($"SIM_INSIDER ({Truncate(r.Message, 100)})");
        activeIncidentId = r.Incident?.Id ?? r.Status?.ActiveIncident?.Id ?? activeIncidentId;
    }
}

// EXECUTE_ROLLBACK
if (!string.IsNullOrWhiteSpace(activeIncidentId))
{
    var r = await Safe("EXECUTE_ROLLBACK", () => client.ExecuteRollbackAsync(activeIncidentId));
    if (r is null) { }
    else if (!r.Success) Fail("EXECUTE_ROLLBACK", r.Error ?? r.Message ?? "failed");
    else Pass($"EXECUTE_ROLLBACK ({activeIncidentId})");
}
else
{
    Fail("EXECUTE_ROLLBACK", "no incident id");
}

// Final vaults/status sanity
{
    var r = await Safe("FINAL_STATUS", () => client.GetStatusAsync());
    if (r is null) { }
    else if (!r.Success || r.Status is null) Fail("FINAL_STATUS", r.Error ?? "null");
    else Pass($"FINAL_STATUS (level={r.Status.Level}, events={r.Status.EventCount})");
}

Console.WriteLine();
Console.WriteLine($"SUMMARY passes={passes} fails={failures.Count}");
foreach (var f in failures)
    Console.WriteLine($"  - {f}");

if (failures.Count > 0)
{
    Console.WriteLine("IPC FULL SUITE FAILED");
    Environment.Exit(1);
}

Console.WriteLine("IPC FULL SUITE PASSED");
return;

static string Truncate(string? s, int n)
{
    if (string.IsNullOrEmpty(s)) return "";
    return s.Length <= n ? s : s[..n] + "...";
}
