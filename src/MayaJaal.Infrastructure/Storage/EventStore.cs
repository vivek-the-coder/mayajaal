using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using MayaJaal.Infrastructure.Cryptography;
using MayaJaal.Shared.Contracts;
using MayaJaal.Shared.Models;

namespace MayaJaal.Infrastructure.Storage;

/// <summary>
/// Thread-safe SQLite event store using Microsoft.Data.Sqlite.
/// </summary>
public sealed class EventStore : IEventStore, IAsyncDisposable, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly string _connectionString;
    private readonly ILogger<EventStore> _logger;
    private readonly HashChain _hashChain;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private long _sequence;
    private bool _initialized;
    private bool _disposed;

    public EventStore(ILogger<EventStore> logger, HashChain hashChain, string? databasePath = null)
    {
        _logger = logger;
        _hashChain = hashChain;

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "MayaJaal",
            "Data");
        Directory.CreateDirectory(root);
        var path = databasePath ?? Path.Combine(root, "events.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized) return;

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            const string schema = """
                CREATE TABLE IF NOT EXISTS events (
                    id TEXT PRIMARY KEY NOT NULL,
                    sequence INTEGER NOT NULL UNIQUE,
                    timestamp TEXT NOT NULL,
                    event_type TEXT NOT NULL,
                    source TEXT NOT NULL,
                    severity INTEGER NOT NULL,
                    is_honey INTEGER NOT NULL DEFAULT 0,
                    is_protected INTEGER NOT NULL DEFAULT 0,
                    risk_contribution INTEGER NOT NULL DEFAULT 0,
                    path TEXT NULL,
                    payload TEXT NOT NULL,
                    hash_chain_prev TEXT NULL,
                    hash_chain_current TEXT NULL,
                    integrity_hash TEXT NULL
                );

                CREATE INDEX IF NOT EXISTS ix_events_timestamp ON events(timestamp DESC);
                CREATE INDEX IF NOT EXISTS ix_events_type ON events(event_type);
                CREATE INDEX IF NOT EXISTS ix_events_sequence ON events(sequence DESC);

                CREATE TABLE IF NOT EXISTS meta (
                    key TEXT PRIMARY KEY NOT NULL,
                    value TEXT NOT NULL
                );
                """;

            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = schema;
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COALESCE(MAX(sequence), 0), (SELECT hash_chain_current FROM events ORDER BY sequence DESC LIMIT 1) FROM events;";
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    _sequence = reader.IsDBNull(0) ? 0 : reader.GetInt64(0);
                    var lastHash = reader.IsDBNull(1) ? null : reader.GetString(1);
                    _hashChain.Reset(lastHash);
                }
            }

            _initialized = true;
            _logger.LogInformation("EventStore initialized at sequence {Sequence}", _sequence);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StoreEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(securityEvent);
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            securityEvent.Sequence = Interlocked.Increment(ref _sequence);
            if (securityEvent.Timestamp == default)
                securityEvent.Timestamp = DateTime.UtcNow;

            _hashChain.Apply(securityEvent);

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO events (
                    id, sequence, timestamp, event_type, source, severity,
                    is_honey, is_protected, risk_contribution, path, payload,
                    hash_chain_prev, hash_chain_current, integrity_hash)
                VALUES (
                    $id, $sequence, $timestamp, $event_type, $source, $severity,
                    $is_honey, $is_protected, $risk_contribution, $path, $payload,
                    $hash_chain_prev, $hash_chain_current, $integrity_hash);
                """;

            cmd.Parameters.AddWithValue("$id", securityEvent.Id);
            cmd.Parameters.AddWithValue("$sequence", securityEvent.Sequence);
            cmd.Parameters.AddWithValue("$timestamp", securityEvent.Timestamp.ToUniversalTime().ToString("O"));
            cmd.Parameters.AddWithValue("$event_type", securityEvent.Type.ToString());
            cmd.Parameters.AddWithValue("$source", securityEvent.Source.ToString());
            cmd.Parameters.AddWithValue("$severity", (int)securityEvent.Severity);
            cmd.Parameters.AddWithValue("$is_honey", securityEvent.IsHoney ? 1 : 0);
            cmd.Parameters.AddWithValue("$is_protected", securityEvent.IsProtected ? 1 : 0);
            cmd.Parameters.AddWithValue("$risk_contribution", securityEvent.RiskContribution);
            cmd.Parameters.AddWithValue("$path", (object?)securityEvent.Path ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(securityEvent, JsonOptions));
            cmd.Parameters.AddWithValue("$hash_chain_prev", (object?)securityEvent.HashChainPrevious ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$hash_chain_current", (object?)securityEvent.HashChainCurrent ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$integrity_hash", (object?)securityEvent.IntegrityHash ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<SecurityEvent>> GetRecentEventsAsync(int count = 100, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        count = Math.Clamp(count, 1, 5000);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT payload FROM events ORDER BY sequence DESC LIMIT $count;";
            cmd.Parameters.AddWithValue("$count", count);
            return await ReadPayloadsAsync(cmd, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<SecurityEvent>> GetEventsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT payload FROM events WHERE timestamp >= $since ORDER BY sequence ASC;";
            cmd.Parameters.AddWithValue("$since", sinceUtc.ToUniversalTime().ToString("O"));
            return await ReadPayloadsAsync(cmd, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<SecurityEvent>> GetEventsByTypeAsync(EventType type, int count = 100, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        count = Math.Clamp(count, 1, 5000);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT payload FROM events WHERE event_type = $type ORDER BY sequence DESC LIMIT $count;";
            cmd.Parameters.AddWithValue("$type", type.ToString());
            cmd.Parameters.AddWithValue("$count", count);
            return await ReadPayloadsAsync(cmd, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<long> GetEventCountAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM events;";
            var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return Convert.ToInt64(result);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string?> GetLastHashAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        return _hashChain.LastHash;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<SecurityEvent>> ReadPayloadsAsync(SqliteCommand cmd, CancellationToken cancellationToken)
    {
        var list = new List<SecurityEvent>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var json = reader.GetString(0);
            var evt = JsonSerializer.Deserialize<SecurityEvent>(json, JsonOptions);
            if (evt is not null)
                list.Add(evt);
        }

        // Queries that use DESC should still present newest-first; ASC queries already chronological.
        return list;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _gate.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
