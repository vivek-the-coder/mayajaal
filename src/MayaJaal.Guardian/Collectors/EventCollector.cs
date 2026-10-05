using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MayaJaal.Shared.Contracts;
using MayaJaal.Shared.Models;

namespace MayaJaal.Guardian.Collectors;

/// <summary>
/// Watches Documents + honey paths, detects mass-file bursts, optionally simulates USB/process signals.
/// </summary>
public sealed class EventCollector : BackgroundService
{
    private readonly IThreatEngine _threatEngine;
    private readonly IHoneyFileService _honeyFileService;
    private readonly IAssetProtectionService _assetProtection;
    private readonly ILogger<EventCollector> _logger;
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly ConcurrentQueue<DateTime> _recentFileOps = new();
    private readonly int _burstThreshold;
    private readonly TimeSpan _burstWindow;
    private readonly bool _enableDemoSignals;
    private DateTime _lastMassEvent = DateTime.MinValue;

    public EventCollector(
        IThreatEngine threatEngine,
        IHoneyFileService honeyFileService,
        IAssetProtectionService assetProtection,
        ILogger<EventCollector> logger,
        int burstThreshold = 25,
        int burstWindowSeconds = 10,
        bool enableDemoSignals = false)
    {
        _threatEngine = threatEngine;
        _honeyFileService = honeyFileService;
        _assetProtection = assetProtection;
        _logger = logger;
        _burstThreshold = burstThreshold;
        _burstWindow = TimeSpan.FromSeconds(burstWindowSeconds);
        _enableDemoSignals = enableDemoSignals;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _honeyFileService.InitializeAsync(stoppingToken).ConfigureAwait(false);

        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(documents) && Directory.Exists(documents))
            AttachWatcher(documents);

        foreach (var honey in await _honeyFileService.GetHoneyFilesAsync(stoppingToken).ConfigureAwait(false))
        {
            var dir = Path.GetDirectoryName(honey.FilePath);
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                AttachWatcher(dir);
        }

        // Ensure Documents is registered as a protected asset for demo baselines.
        if (!string.IsNullOrWhiteSpace(documents))
        {
            await _assetProtection.RegisterAsync(
                documents,
                AssetType.DOCUMENT,
                Environment.UserName,
                sensitivity: 6,
                stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("EventCollector watching {Count} roots", _watchers.Count);

        if (_enableDemoSignals)
            _ = Task.Run(() => RunOptionalDemoSignalsAsync(stoppingToken), stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
    }

    private void AttachWatcher(string path)
    {
        if (_watchers.Any(w => string.Equals(w.Path, path, StringComparison.OrdinalIgnoreCase)))
            return;

        try
        {
            var watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            watcher.Created += (_, e) => _ = OnFileEventAsync(EventType.FILE_COPY, e.FullPath);
            watcher.Changed += (_, e) => _ = OnFileEventAsync(EventType.FILE_MODIFY, e.FullPath);
            watcher.Deleted += (_, e) => _ = OnFileEventAsync(EventType.FILE_DELETE, e.FullPath);
            watcher.Renamed += (_, e) => _ = OnFileEventAsync(EventType.FILE_RENAME, e.FullPath);
            watcher.Error += (_, e) => _logger.LogWarning(e.GetException(), "FileSystemWatcher error on {Path}", path);

            _watchers.Add(watcher);
            _logger.LogDebug("Attached watcher to {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not watch {Path}", path);
        }
    }

    private async Task OnFileEventAsync(EventType type, string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) return;

            var isHoney = _honeyFileService.IsHoneyPath(path);
            var isProtected = _assetProtection.IsProtected(path);

            if (isHoney)
                type = type is EventType.FILE_MODIFY or EventType.FILE_DELETE or EventType.FILE_RENAME
                    ? EventType.HONEY_MODIFY
                    : EventType.HONEY_ACCESS;

            NoteFileOperation();
            var mass = MaybeCreateMassActivityEvent();

            var evt = new SecurityEvent
            {
                Type = type,
                Source = isHoney ? EventSource.DECEPTION : EventSource.FILE_SYSTEM,
                Severity = isHoney ? EventSeverity.CRITICAL : isProtected ? EventSeverity.HIGH : EventSeverity.MEDIUM,
                IsHoney = isHoney,
                IsProtected = isProtected,
                RiskContribution = isHoney ? 60 : type == EventType.FILE_DELETE ? 20 : 8,
                User = new UserContext { UserName = Environment.UserName, UserId = Environment.UserName },
                File = new FileContext
                {
                    Path = path,
                    Extension = Path.GetExtension(path),
                    IsHoney = isHoney,
                    IsProtected = isProtected,
                    SensitivityLevel = isHoney ? 9 : isProtected ? 7 : 5,
                    ModifiedAt = DateTime.UtcNow
                }
            };

            await _threatEngine.ProcessEventAsync(evt).ConfigureAwait(false);
            if (mass is not null)
                await _threatEngine.ProcessEventAsync(mass).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed processing file event for {Path}", path);
        }
    }

    private void NoteFileOperation()
    {
        var now = DateTime.UtcNow;
        _recentFileOps.Enqueue(now);
        while (_recentFileOps.TryPeek(out var oldest) && now - oldest > _burstWindow)
            _recentFileOps.TryDequeue(out _);
    }

    private SecurityEvent? MaybeCreateMassActivityEvent()
    {
        var now = DateTime.UtcNow;
        if (_recentFileOps.Count < _burstThreshold) return null;
        if (now - _lastMassEvent < TimeSpan.FromSeconds(5)) return null;
        _lastMassEvent = now;

        return new SecurityEvent
        {
            Type = EventType.MASS_FILE_ACTIVITY,
            Source = EventSource.FILE_SYSTEM,
            Severity = EventSeverity.HIGH,
            RiskContribution = 40,
            User = new UserContext { UserName = Environment.UserName, UserId = Environment.UserName },
            Metadata = { ["ops"] = _recentFileOps.Count, ["windowSeconds"] = _burstWindow.TotalSeconds }
        };
    }

    private async Task RunOptionalDemoSignalsAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken).ConfigureAwait(false);
            await _threatEngine.ProcessEventAsync(new SecurityEvent
            {
                Type = EventType.USB_INSERT,
                Source = EventSource.USB_MANAGER,
                Severity = EventSeverity.MEDIUM,
                RiskContribution = 12,
                Device = new DeviceContext
                {
                    DeviceId = "demo-usb-001",
                    DeviceName = "Demo USB Drive",
                    DeviceType = "USB",
                    TotalSize = 64L * 1024 * 1024 * 1024
                },
                User = new UserContext { UserName = Environment.UserName }
            }, stoppingToken).ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
            await _threatEngine.ProcessEventAsync(new SecurityEvent
            {
                Type = EventType.PROCESS_START,
                Source = EventSource.PROCESS_MANAGER,
                Severity = EventSeverity.MEDIUM,
                RiskContribution = 10,
                Process = new ProcessContext
                {
                    ProcessId = Environment.ProcessId,
                    ProcessName = "robocopy.exe",
                    IsSuspicious = true,
                    StartTime = DateTime.UtcNow
                },
                User = new UserContext { UserName = Environment.UserName }
            }, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }

    public override void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
        base.Dispose();
    }
}
