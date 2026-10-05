using System.IO.Pipes;
using System.Text.Json;

namespace MayaJaal.Shared.IPC;

/// <summary>
/// Named-pipe client using length-prefixed UTF-8 JSON framing:
/// [4-byte little-endian length][UTF-8 JSON payload].
/// </summary>
public sealed class GuardianClient : IAsyncDisposable, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly string _pipeName;
    private readonly TimeSpan _connectTimeout;
    private NamedPipeClientStream? _pipe;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public GuardianClient(string? pipeName = null, TimeSpan? connectTimeout = null)
    {
        _pipeName = pipeName ?? IpcDefaults.PipeName;
        _connectTimeout = connectTimeout ?? TimeSpan.FromSeconds(3);
    }

    public bool IsConnected => _pipe?.IsConnected == true;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsConnected) return;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsConnected) return;
            _pipe?.Dispose();
            _pipe = new NamedPipeClientStream(
                ".",
                _pipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(_connectTimeout);
            await _pipe.ConnectAsync(linked.Token).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IpcResponse> SendAsync(IpcRequest request, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);

        if (!IsConnected)
            await ConnectAsync(cancellationToken).ConfigureAwait(false);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_pipe is null || !_pipe.IsConnected)
                throw new InvalidOperationException("Guardian named pipe is not connected.");

            await WriteMessageAsync(_pipe, request, cancellationToken).ConfigureAwait(false);
            return await ReadMessageAsync<IpcResponse>(_pipe, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<IpcResponse> GetStatusAsync(CancellationToken cancellationToken = default)
        => SendAsync(new IpcRequest { Command = IpcCommands.GetStatus }, cancellationToken);

    public Task<IpcResponse> GetIncidentAsync(CancellationToken cancellationToken = default)
        => SendAsync(new IpcRequest { Command = IpcCommands.GetIncident }, cancellationToken);

    public Task<IpcResponse> GetEventsAsync(int count = 50, CancellationToken cancellationToken = default)
        => SendAsync(new IpcRequest { Command = IpcCommands.GetEvents, Count = count }, cancellationToken);

    public Task<IpcResponse> GetAssetsAsync(CancellationToken cancellationToken = default)
        => SendAsync(new IpcRequest { Command = IpcCommands.GetAssets }, cancellationToken);

    public Task<IpcResponse> LockVaultAsync(string? vaultId = null, CancellationToken cancellationToken = default)
        => SendAsync(new IpcRequest { Command = IpcCommands.LockVault, VaultId = vaultId }, cancellationToken);

    public Task<IpcResponse> GetVaultsAsync(CancellationToken cancellationToken = default)
        => SendAsync(new IpcRequest { Command = IpcCommands.GetVaults }, cancellationToken);

    public Task<IpcResponse> GetIncidentsAsync(int count = 50, CancellationToken cancellationToken = default)
        => SendAsync(new IpcRequest { Command = IpcCommands.GetIncidents, Count = count }, cancellationToken);

    public Task<IpcResponse> ResolveIncidentAsync(
        string incidentId,
        string? resolvedBy = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
        => SendAsync(new IpcRequest
        {
            Command = IpcCommands.ResolveIncident,
            IncidentId = incidentId,
            ResolvedBy = resolvedBy,
            Notes = notes
        }, cancellationToken);

    public Task<IpcResponse> GetPoliciesAsync(CancellationToken cancellationToken = default)
        => SendAsync(new IpcRequest { Command = IpcCommands.GetPolicies }, cancellationToken);

    public Task<IpcResponse> ExecuteRollbackAsync(string incidentId, CancellationToken cancellationToken = default)
        => SendAsync(new IpcRequest { Command = IpcCommands.ExecuteRollback, IncidentId = incidentId }, cancellationToken);

    public Task<IpcResponse> RunAttackSimulationAsync(string? scenario = null, CancellationToken cancellationToken = default)
        => SendAsync(new IpcRequest
        {
            Command = IpcCommands.RunAttackSimulation,
            Scenario = scenario
        }, cancellationToken);

    public Task<IpcResponse> AddVaultItemAsync(string? vaultId, string sourcePath, CancellationToken cancellationToken = default)
        => SendAsync(new IpcRequest
        {
            Command = IpcCommands.AddVaultItem,
            VaultId = vaultId,
            SourcePath = sourcePath
        }, cancellationToken);

    public Task<IpcResponse> UnlockVaultAsync(string? vaultId = null, CancellationToken cancellationToken = default)
        => SendAsync(new IpcRequest
        {
            Command = IpcCommands.UnlockVault,
            VaultId = vaultId
        }, cancellationToken);

    public static async Task WriteMessageAsync<T>(Stream stream, T message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
        if (json.Length > IpcDefaults.MaxMessageBytes)
            throw new InvalidOperationException($"IPC message exceeds {IpcDefaults.MaxMessageBytes} bytes.");

        var length = BitConverter.GetBytes(json.Length);
        await stream.WriteAsync(length, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(json, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<T> ReadMessageAsync<T>(Stream stream, CancellationToken cancellationToken = default)
    {
        var lengthBuffer = new byte[4];
        await ReadExactAsync(stream, lengthBuffer, cancellationToken).ConfigureAwait(false);
        var length = BitConverter.ToInt32(lengthBuffer, 0);
        if (length <= 0 || length > IpcDefaults.MaxMessageBytes)
            throw new InvalidDataException($"Invalid IPC frame length: {length}");

        var payload = new byte[length];
        await ReadExactAsync(stream, payload, cancellationToken).ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<T>(payload, JsonOptions);
        if (result is null)
            throw new InvalidDataException("Failed to deserialize IPC payload.");
        return result;
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
                throw new EndOfStreamException("Named pipe closed while reading IPC frame.");
            offset += read;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pipe?.Dispose();
        _gate.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

