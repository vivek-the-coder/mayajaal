using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MayaJaal.Shared.Contracts;
using MayaJaal.Shared.Models;

namespace MayaJaal.Guardian.Collectors;

/// <summary>
/// Watches for new process starts. Emits PROCESS_START only for suspicious names
/// to avoid flooding the threat window with normal desktop activity.
/// </summary>
public sealed class ProcessCollector : BackgroundService
{
    private readonly IThreatEngine _threatEngine;
    private readonly ILogger<ProcessCollector> _logger;
    private readonly ConcurrentDictionary<int, byte> _knownPids = new();

    private static readonly HashSet<string> Suspicious = new(StringComparer.OrdinalIgnoreCase)
    {
        "cmd", "powershell", "pwsh", "wscript", "cscript",
        "mshta", "rundll32", "regsvr32", "reg",
        "schtasks", "wmic", "net", "net1",
        "whoami", "certutil", "bitsadmin",
        "curl", "wget", "certreq",
        "robocopy", "xcopy", "rclone", "megasync"
    };

    public ProcessCollector(IThreatEngine threatEngine, ILogger<ProcessCollector> logger)
    {
        _threatEngine = threatEngine;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Seed();
        _logger.LogInformation("Process Collector started ({Count} PIDs seeded; suspicious-only emission)", _knownPids.Count);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                await ScanAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
    }

    private void Seed()
    {
        foreach (var proc in SafeGetProcesses())
            _knownPids.TryAdd(proc.Id, 0);
    }

    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        try
        {
            var live = SafeGetProcesses();
            var liveIds = new HashSet<int>(live.Select(p => p.Id));

            foreach (var proc in live)
            {
                if (!_knownPids.TryAdd(proc.Id, 0))
                    continue;

                string name;
                try { name = proc.ProcessName; }
                catch { continue; }

                if (!Suspicious.Contains(name))
                    continue;

                string? path = null;
                DateTime start = DateTime.UtcNow;
                try
                {
                    path = proc.MainModule?.FileName;
                    start = proc.StartTime.ToUniversalTime();
                }
                catch
                {
                    // Access denied for some system processes
                }

                await _threatEngine.ProcessEventAsync(new SecurityEvent
                {
                    Type = EventType.PROCESS_START,
                    Source = EventSource.PROCESS_MANAGER,
                    Severity = EventSeverity.HIGH,
                    RiskContribution = 12,
                    Process = new ProcessContext
                    {
                        ProcessId = proc.Id,
                        ProcessName = name + (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? "" : ".exe"),
                        ProcessPath = path,
                        IsSuspicious = true,
                        StartTime = start
                    },
                    User = new UserContext { UserName = Environment.UserName, UserId = Environment.UserName }
                }, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Suspicious process start: {Name} (PID {Pid})", name, proc.Id);
            }

            foreach (var pid in _knownPids.Keys.Where(id => !liveIds.Contains(id)).ToList())
                _knownPids.TryRemove(pid, out _);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Process scan iteration failed");
        }
    }

    private static Process[] SafeGetProcesses()
    {
        try { return Process.GetProcesses(); }
        catch { return []; }
    }
}
