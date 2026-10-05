using MayaJaal.Shared.IPC;
using MayaJaal.Shared.Models;

namespace MayaJaal.SecurityCenter.Services;

/// <summary>
/// Thin wrapper around <see cref="GuardianClient"/> for the Security Center UI.
/// </summary>
public sealed class GuardianStatusService : IAsyncDisposable, IDisposable
{
    private GuardianClient _client;
    private bool _disposed;

    public GuardianStatusService(GuardianClient? client = null)
    {
        _client = client ?? new GuardianClient();
    }

    public bool IsConnected => _client.IsConnected;
    public bool LastRefreshSucceeded { get; private set; }
    public string? LastError { get; private set; }

    public async Task<GuardianSnapshot> RefreshAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            if (!_client.IsConnected)
                await _client.ConnectAsync(cancellationToken).ConfigureAwait(false);

            var statusResponse = await _client.GetStatusAsync(cancellationToken).ConfigureAwait(false);
            var eventsResponse = await _client.GetEventsAsync(50, cancellationToken).ConfigureAwait(false);
            var incidentResponse = await _client.GetIncidentAsync(cancellationToken).ConfigureAwait(false);
            var vaultsResponse = await _client.GetVaultsAsync(cancellationToken).ConfigureAwait(false);
            var incidentsResponse = await _client.GetIncidentsAsync(50, cancellationToken).ConfigureAwait(false);
            var policiesResponse = await _client.GetPoliciesAsync(cancellationToken).ConfigureAwait(false);

            var status = statusResponse.Success ? statusResponse.Status : null;
            var events = eventsResponse.Success
                ? eventsResponse.Events ?? status?.RecentEvents ?? []
                : status?.RecentEvents ?? [];
            var incident = incidentResponse.Success
                ? incidentResponse.Incident ?? status?.ActiveIncident
                : status?.ActiveIncident;

            LastRefreshSucceeded = statusResponse.Success;
            LastError = statusResponse.Success
                ? null
                : statusResponse.Error ?? "Guardian returned an unsuccessful status response.";

            return new GuardianSnapshot(
                Online: LastRefreshSucceeded && (status?.GuardianOnline ?? true),
                Status: status,
                ActiveIncident: incident,
                Events: events.OrderByDescending(e => e.Timestamp).ToList(),
                Vaults: vaultsResponse.Success ? vaultsResponse.Vaults ?? [] : [],
                Incidents: incidentsResponse.Success ? incidentsResponse.Incidents ?? [] : [],
                Policies: policiesResponse.Success ? policiesResponse.Policies ?? [] : [],
                Error: LastError);
        }
        catch (Exception ex)
        {
            LastRefreshSucceeded = false;
            LastError = ex.Message;
            ResetClient();

            return new GuardianSnapshot(
                Online: false,
                Status: null,
                ActiveIncident: null,
                Events: [],
                Vaults: [],
                Incidents: [],
                Policies: [],
                Error: LastError);
        }
    }

    private void ResetClient()
    {
        try { _client.Dispose(); } catch { /* ignore */ }
        _client = new GuardianClient();
    }

    public async Task<bool> LockVaultAsync(string? vaultId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_client.IsConnected)
                await _client.ConnectAsync(cancellationToken).ConfigureAwait(false);

            var response = await _client.LockVaultAsync(vaultId, cancellationToken).ConfigureAwait(false);
            return response.Success;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ResolveIncidentAsync(string incidentId, string? notes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_client.IsConnected)
                await _client.ConnectAsync(cancellationToken).ConfigureAwait(false);

            var response = await _client.ResolveIncidentAsync(incidentId, Environment.UserName, notes, cancellationToken)
                .ConfigureAwait(false);
            return response.Success;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExecuteRollbackAsync(string incidentId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_client.IsConnected)
                await _client.ConnectAsync(cancellationToken).ConfigureAwait(false);

            var response = await _client.ExecuteRollbackAsync(incidentId, cancellationToken).ConfigureAwait(false);
            return response.Success;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Ok, string Message)> RunAttackSimulationAsync(
        string scenario = "usb-exfil",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_client.IsConnected)
                await _client.ConnectAsync(cancellationToken).ConfigureAwait(false);

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(TimeSpan.FromSeconds(45));
            var response = await _client.RunAttackSimulationAsync(scenario, linked.Token).ConfigureAwait(false);
            var message = response.Success
                ? (response.Message ?? "Attack simulation completed.")
                : (response.Error ?? "Attack simulation failed.");
            return (response.Success, message);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Ok, string Message)> UnlockVaultAsync(
        string? vaultId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_client.IsConnected)
                await _client.ConnectAsync(cancellationToken).ConfigureAwait(false);

            var response = await _client.UnlockVaultAsync(vaultId, cancellationToken).ConfigureAwait(false);
            return (response.Success, response.Success
                ? (response.Message ?? "Vault unlocked.")
                : (response.Error ?? "Unlock failed."));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Ok, string Message)> AddVaultItemAsync(
        string sourcePath,
        string? vaultId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_client.IsConnected)
                await _client.ConnectAsync(cancellationToken).ConfigureAwait(false);

            var response = await _client.AddVaultItemAsync(vaultId, sourcePath, cancellationToken).ConfigureAwait(false);
            return (response.Success, response.Success
                ? (response.Message ?? "File added to vault.")
                : (response.Error ?? "Add file failed."));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _client.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

public sealed record GuardianSnapshot(
    bool Online,
    ThreatState? Status,
    Incident? ActiveIncident,
    IReadOnlyList<SecurityEvent> Events,
    IReadOnlyList<VaultSummary> Vaults,
    IReadOnlyList<Incident> Incidents,
    IReadOnlyList<PolicySummary> Policies,
    string? Error);
