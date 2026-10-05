using System.IO.Pipes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MayaJaal.Domain.Services;
using MayaJaal.Shared.Contracts;
using MayaJaal.Shared.IPC;
using MayaJaal.Shared.Models;

namespace MayaJaal.Guardian.IPC;

/// <summary>
/// Named-pipe server using ACL-hardened pipes and length-prefixed JSON framing.
/// </summary>
public sealed class IpcServer : BackgroundService
{
    private readonly IThreatEngine _threatEngine;
    private readonly IAssetProtectionService _assetProtection;
    private readonly IVaultService _vaultService;
    private readonly IIncidentService _incidentService;
    private readonly IPolicyEngine _policyEngine;
    private readonly DemoScenarioRunner _demoScenarioRunner;
    private readonly ILogger<IpcServer> _logger;
    private readonly string _pipeName;

    public IpcServer(
        IThreatEngine threatEngine,
        IAssetProtectionService assetProtection,
        IVaultService vaultService,
        IIncidentService incidentService,
        IPolicyEngine policyEngine,
        DemoScenarioRunner demoScenarioRunner,
        ILogger<IpcServer> logger,
        string? pipeName = null)
    {
        _threatEngine = threatEngine;
        _assetProtection = assetProtection;
        _vaultService = vaultService;
        _incidentService = incidentService;
        _policyEngine = policyEngine;
        _demoScenarioRunner = demoScenarioRunner;
        _logger = logger;
        _pipeName = pipeName ?? IpcDefaults.PipeName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IPC server listening on secured pipe {Pipe}", _pipeName);
        while (!stoppingToken.IsCancellationRequested)
        {
            NamedPipeServerStream? server = null;
            try
            {
                server = SecurePipeFactory.Create(_pipeName);
                await server.WaitForConnectionAsync(stoppingToken).ConfigureAwait(false);

                if (!SecurePipeFactory.IsConnectedClientAuthorized(server))
                {
                    _logger.LogWarning("Rejected unauthorized IPC client on {Pipe}", _pipeName);
                    await server.DisposeAsync().ConfigureAwait(false);
                    continue;
                }

                _ = HandleClientAsync(server, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                if (server is not null)
                    await server.DisposeAsync().ConfigureAwait(false);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "IPC accept loop error");
                if (server is not null)
                    await server.DisposeAsync().ConfigureAwait(false);
                await Task.Delay(500, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream server, CancellationToken stoppingToken)
    {
        try
        {
            await using (server)
            {
                while (server.IsConnected && !stoppingToken.IsCancellationRequested)
                {
                    IpcRequest request;
                    try
                    {
                        request = await GuardianClient.ReadMessageAsync<IpcRequest>(server, stoppingToken)
                            .ConfigureAwait(false);
                    }
                    catch (EndOfStreamException)
                    {
                        break;
                    }

                    var response = await DispatchAsync(request, stoppingToken).ConfigureAwait(false);
                    response.Id = request.Id;
                    await GuardianClient.WriteMessageAsync(server, response, stoppingToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutting down
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "IPC client handler ended");
        }
    }

    private async Task<IpcResponse> DispatchAsync(IpcRequest request, CancellationToken cancellationToken)
    {
        try
        {
            switch (request.Command?.ToUpperInvariant())
            {
                case IpcCommands.Ping:
                    return new IpcResponse { Success = true, Message = "PONG" };

                case IpcCommands.GetStatus:
                    return new IpcResponse
                    {
                        Success = true,
                        Status = _threatEngine.GetCurrentState()
                    };

                case IpcCommands.GetIncident:
                    return new IpcResponse
                    {
                        Success = true,
                        Incident = _threatEngine.GetActiveIncident()
                            ?? await _incidentService.GetActiveIncidentAsync(cancellationToken).ConfigureAwait(false)
                    };

                case IpcCommands.GetEvents:
                    return new IpcResponse
                    {
                        Success = true,
                        Events = _threatEngine.GetRecentEvents(request.Count <= 0 ? 50 : request.Count).ToList()
                    };

                case IpcCommands.GetAssets:
                    var assets = await _assetProtection.GetAssetsAsync(cancellationToken).ConfigureAwait(false);
                    return new IpcResponse
                    {
                        Success = true,
                        Assets = assets.ToList()
                    };

                case IpcCommands.GetVaults:
                    var vaults = await _vaultService.ListVaultsAsync(cancellationToken).ConfigureAwait(false);
                    return new IpcResponse
                    {
                        Success = true,
                        Vaults = vaults.Select(v => new VaultSummary
                        {
                            Id = v.Id,
                            Name = v.Name,
                            State = (_vaultService.IsUnlocked(v.Id) ? VaultState.UNLOCKED : v.State).ToString(),
                            ItemCount = v.ItemCount,
                            TotalSize = v.TotalSize,
                            OwnerId = v.OwnerId,
                            Items = v.Items.Select(i => new VaultItemSummary
                            {
                                Id = i.Id,
                                Name = i.Name,
                                Size = i.Size,
                                OriginalPath = i.OriginalPath,
                                AddedAt = i.AddedAt
                            }).ToList()
                        }).ToList()
                    };

                case IpcCommands.GetIncidents:
                    var incidents = await _incidentService.ListIncidentsAsync(
                        request.Count <= 0 ? 50 : request.Count,
                        cancellationToken).ConfigureAwait(false);
                    return new IpcResponse
                    {
                        Success = true,
                        Incidents = incidents.ToList()
                    };

                case IpcCommands.ResolveIncident:
                    if (string.IsNullOrWhiteSpace(request.IncidentId))
                        return new IpcResponse { Success = false, Error = "incidentId is required" };

                    await _incidentService.ResolveIncidentAsync(
                        request.IncidentId,
                        string.IsNullOrWhiteSpace(request.ResolvedBy) ? Environment.UserName : request.ResolvedBy!,
                        request.Notes,
                        cancellationToken).ConfigureAwait(false);
                    _threatEngine.ClearActiveIncident(request.IncidentId);

                    return new IpcResponse
                    {
                        Success = true,
                        Message = $"Incident {request.IncidentId} resolved",
                        Status = _threatEngine.GetCurrentState()
                    };

                case IpcCommands.GetPolicies:
                    var policy = _policyEngine.GetDefaultPolicy();
                    return new IpcResponse
                    {
                        Success = true,
                        Policies =
                        [
                            new PolicySummary
                            {
                                Id = policy.Id,
                                Name = policy.Name,
                                Description = policy.Description,
                                IsEnabled = policy.IsEnabled,
                                Priority = policy.Priority,
                                DefaultAction = policy.DefaultAction.Action.ToString(),
                                Version = policy.Version
                            }
                        ]
                    };

                case IpcCommands.ExecuteRollback:
                    if (string.IsNullOrWhiteSpace(request.IncidentId))
                        return new IpcResponse { Success = false, Error = "incidentId is required" };

                    await _incidentService.ExecuteRollbackAsync(request.IncidentId, cancellationToken)
                        .ConfigureAwait(false);
                    _threatEngine.ClearActiveIncident(request.IncidentId);
                    return new IpcResponse
                    {
                        Success = true,
                        Message = $"Rollback executed for {request.IncidentId}",
                        Status = _threatEngine.GetCurrentState()
                    };

                case IpcCommands.LockVault:
                    if (!string.IsNullOrWhiteSpace(request.VaultId))
                        await _vaultService.LockVaultAsync(request.VaultId, cancellationToken).ConfigureAwait(false);
                    else
                        await _vaultService.LockAllAsync(cancellationToken).ConfigureAwait(false);

                    return new IpcResponse
                    {
                        Success = true,
                        Message = string.IsNullOrWhiteSpace(request.VaultId)
                            ? "All vaults locked"
                            : $"Vault {request.VaultId} locked",
                        Status = _threatEngine.GetCurrentState()
                    };

                case IpcCommands.RunAttackSimulation:
                    var summary = await _demoScenarioRunner
                        .RunScenarioAsync(request.Scenario, cancellationToken)
                        .ConfigureAwait(false);
                    return new IpcResponse
                    {
                        Success = true,
                        Message = summary,
                        Status = _threatEngine.GetCurrentState(),
                        Incident = _threatEngine.GetActiveIncident()
                            ?? await _incidentService.GetActiveIncidentAsync(cancellationToken).ConfigureAwait(false)
                    };

                case IpcCommands.UnlockVault:
                {
                    await VaultBootstrapper.EnsureDefaultVaultAsync(_vaultService, _logger, cancellationToken)
                        .ConfigureAwait(false);
                    var vaultId = await ResolveVaultIdAsync(request.VaultId, cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(vaultId))
                        return new IpcResponse { Success = false, Error = "No vault found." };

                    var unlocked = _vaultService.IsUnlocked(vaultId);
                    return new IpcResponse
                    {
                        Success = unlocked,
                        Message = unlocked
                            ? $"Vault unlocked: {vaultId}"
                            : "Could not unlock vault. Bootstrap secret may be missing — restart Guardian once.",
                        Status = _threatEngine.GetCurrentState()
                    };
                }

                case IpcCommands.AddVaultItem:
                {
                    if (string.IsNullOrWhiteSpace(request.SourcePath))
                        return new IpcResponse { Success = false, Error = "sourcePath is required" };

                    if (!File.Exists(request.SourcePath))
                        return new IpcResponse { Success = false, Error = "Source file not found." };

                    await VaultBootstrapper.EnsureDefaultVaultAsync(_vaultService, _logger, cancellationToken)
                        .ConfigureAwait(false);
                    var vaultId = await ResolveVaultIdAsync(request.VaultId, cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(vaultId))
                        return new IpcResponse { Success = false, Error = "No vault found. Unlock vault first." };

                    if (!_vaultService.IsUnlocked(vaultId))
                        return new IpcResponse
                        {
                            Success = false,
                            Error = "Vault is locked. Click Unlock vault first, then add the file."
                        };

                    var item = await _vaultService.AddItemAsync(vaultId, request.SourcePath, cancellationToken)
                        .ConfigureAwait(false);
                    return new IpcResponse
                    {
                        Success = true,
                        Message = $"Added '{item.Name}' to vault ({item.Size} bytes encrypted).",
                        Status = _threatEngine.GetCurrentState()
                    };
                }

                default:
                    return new IpcResponse
                    {
                        Success = false,
                        Error = $"Unknown command: {request.Command}"
                    };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IPC command {Command} failed", request.Command);
            return new IpcResponse { Success = false, Error = ex.Message };
        }
    }

    private async Task<string?> ResolveVaultIdAsync(string? requestedVaultId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(requestedVaultId))
            return requestedVaultId;

        var vaults = await _vaultService.ListVaultsAsync(cancellationToken).ConfigureAwait(false);
        return vaults.FirstOrDefault()?.Id;
    }
}
