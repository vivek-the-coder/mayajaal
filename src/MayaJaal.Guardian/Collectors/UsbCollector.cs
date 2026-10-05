using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MayaJaal.Shared.Contracts;
using MayaJaal.Shared.Models;

namespace MayaJaal.Guardian.Collectors;

/// <summary>
/// Polls removable drives and emits USB_INSERT / USB_REMOVE into the threat pipeline.
/// </summary>
public sealed class UsbCollector : BackgroundService
{
    private readonly IThreatEngine _threatEngine;
    private readonly ILogger<UsbCollector> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _knownDevices = new(StringComparer.OrdinalIgnoreCase);

    public UsbCollector(IThreatEngine threatEngine, ILogger<UsbCollector> logger)
    {
        _threatEngine = threatEngine;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Seed current removable drives so we only emit true inserts after start.
        foreach (var drive in GetRemovableReady())
            _knownDevices[drive.Name] = DateTime.UtcNow;

        _logger.LogInformation("USB Collector started ({Count} existing removable drives seeded)", _knownDevices.Count);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
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

    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        try
        {
            var drives = GetRemovableReady();
            var present = new HashSet<string>(drives.Select(d => d.Name), StringComparer.OrdinalIgnoreCase);

            foreach (var drive in drives)
            {
                if (_knownDevices.ContainsKey(drive.Name))
                    continue;

                _knownDevices[drive.Name] = DateTime.UtcNow;
                long total = 0, free = 0;
                string label = "USB Drive";
                try
                {
                    total = drive.TotalSize;
                    free = drive.AvailableFreeSpace;
                    label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "USB Drive" : drive.VolumeLabel;
                }
                catch
                {
                    // Volume may disappear mid-read
                }

                var device = new DeviceContext
                {
                    DeviceId = drive.Name,
                    DeviceName = label,
                    DeviceType = "USB",
                    TotalSize = total,
                    FreeSize = free
                };
                EnrichUsbIdentity(drive.Name, device);

                await _threatEngine.ProcessEventAsync(new SecurityEvent
                {
                    Type = EventType.USB_INSERT,
                    Source = EventSource.USB_MANAGER,
                    Severity = EventSeverity.HIGH,
                    RiskContribution = 15,
                    Device = device,
                    User = new UserContext { UserName = Environment.UserName, UserId = Environment.UserName }
                }, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("USB insert detected: {Drive}", drive.Name);
            }

            foreach (var key in _knownDevices.Keys.Where(k => !present.Contains(k)).ToList())
            {
                if (!_knownDevices.TryRemove(key, out _))
                    continue;

                await _threatEngine.ProcessEventAsync(new SecurityEvent
                {
                    Type = EventType.USB_REMOVE,
                    Source = EventSource.USB_MANAGER,
                    Severity = EventSeverity.MEDIUM,
                    RiskContribution = 5,
                    Device = new DeviceContext
                    {
                        DeviceId = key,
                        DeviceName = key,
                        DeviceType = "USB"
                    },
                    User = new UserContext { UserName = Environment.UserName, UserId = Environment.UserName }
                }, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("USB remove detected: {Drive}", key);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "USB scan iteration failed");
        }
    }

    private static List<DriveInfo> GetRemovableReady()
    {
        try
        {
            return DriveInfo.GetDrives()
                .Where(d =>
                {
                    try { return d.DriveType == DriveType.Removable && d.IsReady; }
                    catch { return false; }
                })
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static void EnrichUsbIdentity(string driveRoot, DeviceContext device)
    {
        try
        {
            var letter = driveRoot.TrimEnd('\\');
            using var searcher = new System.Management.ManagementObjectSearcher(
                $"SELECT VolumeSerialNumber, FileSystem FROM Win32_LogicalDisk WHERE DeviceID='{letter}'");
            foreach (System.Management.ManagementObject disk in searcher.Get())
            {
                device.SerialNumber ??= disk["VolumeSerialNumber"]?.ToString();
                if (string.IsNullOrWhiteSpace(device.VendorId))
                    device.VendorId = disk["FileSystem"]?.ToString();
                break;
            }
        }
        catch
        {
            // WMI may be unavailable; DriveInfo identity remains.
        }
    }
}
