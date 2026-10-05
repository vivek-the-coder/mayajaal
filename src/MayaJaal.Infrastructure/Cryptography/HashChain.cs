using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MayaJaal.Shared.Models;

namespace MayaJaal.Infrastructure.Cryptography;

/// <summary>
/// SHA-256 hash chain for append-only evidence integrity.
/// </summary>
public sealed class HashChain
{
    private static readonly JsonSerializerOptions CompactJson = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private string? _lastHash;
    private readonly object _sync = new();

    public string? LastHash
    {
        get { lock (_sync) return _lastHash; }
    }

    public void Reset(string? lastHash = null)
    {
        lock (_sync) _lastHash = lastHash;
    }

    public void Apply(SecurityEvent securityEvent)
    {
        ArgumentNullException.ThrowIfNull(securityEvent);
        lock (_sync)
        {
            securityEvent.HashChainPrevious = _lastHash;
            securityEvent.HashChainCurrent = ComputeNext(_lastHash, securityEvent);
            securityEvent.IntegrityHash = securityEvent.HashChainCurrent;
            _lastHash = securityEvent.HashChainCurrent;
        }
    }

    public static string ComputeNext(string? previousHash, SecurityEvent securityEvent)
    {
        var payload = new
        {
            previous = previousHash ?? "GENESIS",
            id = securityEvent.Id,
            sequence = securityEvent.Sequence,
            timestamp = securityEvent.Timestamp.ToUniversalTime().ToString("O"),
            type = securityEvent.Type.ToString(),
            source = securityEvent.Source.ToString(),
            severity = (int)securityEvent.Severity,
            path = securityEvent.Path,
            isHoney = securityEvent.IsHoney,
            risk = securityEvent.RiskContribution
        };

        var json = JsonSerializer.Serialize(payload, CompactJson);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool Verify(SecurityEvent securityEvent, string? expectedPrevious)
    {
        if (!string.Equals(securityEvent.HashChainPrevious, expectedPrevious, StringComparison.OrdinalIgnoreCase))
            return false;

        var expected = ComputeNext(expectedPrevious, securityEvent);
        return string.Equals(securityEvent.HashChainCurrent, expected, StringComparison.OrdinalIgnoreCase);
    }
}
