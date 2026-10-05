using Microsoft.Extensions.Logging;
using MayaJaal.Shared.Contracts;
using MayaJaal.Shared.Models;

namespace MayaJaal.Guardian;

/// <summary>
/// Faculty/demo attacker scenarios: USB exfiltration and insider credential harvest.
/// </summary>
public sealed class DemoScenarioRunner
{
    private readonly IThreatEngine _threatEngine;
    private readonly IHoneyFileService _honeyFileService;
    private readonly ILogger<DemoScenarioRunner> _logger;
    private readonly SemaphoreSlim _runGate = new(1, 1);

    public DemoScenarioRunner(
        IThreatEngine threatEngine,
        IHoneyFileService honeyFileService,
        ILogger<DemoScenarioRunner> logger)
    {
        _threatEngine = threatEngine;
        _honeyFileService = honeyFileService;
        _logger = logger;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
        => RunScenarioAsync("usb-exfil", cancellationToken);

    public async Task<string> RunScenarioAsync(string? scenario, CancellationToken cancellationToken = default)
    {
        if (!await _runGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            return "Attack simulation already running. Wait for it to finish.";

        try
        {
            var key = (scenario ?? "usb-exfil").Trim().ToLowerInvariant();
            return key switch
            {
                "insider" or "insider-harvest" => await RunInsiderHarvestAsync(cancellationToken).ConfigureAwait(false),
                _ => await RunUsbExfilAsync(cancellationToken).ConfigureAwait(false)
            };
        }
        finally
        {
            _runGate.Release();
        }
    }

    private async Task<string> RunUsbExfilAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("Attack simulation: USB exfiltration (USB → process → honey → mass copy)");

        await _honeyFileService.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var honey = (await _honeyFileService.GetHoneyFilesAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault();

        await EmitAsync(new SecurityEvent
        {
            Type = EventType.USB_INSERT,
            Source = EventSource.USB_MANAGER,
            Severity = EventSeverity.MEDIUM,
            RiskContribution = 15,
            Device = new DeviceContext
            {
                DeviceId = "demo-usb",
                DeviceName = "Kingston DataTraveler",
                DeviceType = "USB",
                SerialNumber = "DEMO-SERIAL-001"
            },
            User = Actor()
        }, cancellationToken).ConfigureAwait(false);

        await Task.Delay(550, cancellationToken).ConfigureAwait(false);

        await EmitAsync(new SecurityEvent
        {
            Type = EventType.PROCESS_START,
            Source = EventSource.PROCESS_MANAGER,
            Severity = EventSeverity.MEDIUM,
            RiskContribution = 12,
            Process = new ProcessContext
            {
                ProcessId = 4242,
                ProcessName = "exfil-tool.exe",
                ProcessPath = @"C:\Users\Public\exfil-tool.exe",
                IsSuspicious = true,
                StartTime = DateTime.UtcNow
            },
            User = Actor()
        }, cancellationToken).ConfigureAwait(false);

        await Task.Delay(550, cancellationToken).ConfigureAwait(false);

        if (honey is not null)
        {
            var honeyFile = new FileContext
            {
                Path = honey.FilePath,
                IsHoney = true,
                SensitivityLevel = 9,
                Extension = System.IO.Path.GetExtension(honey.FilePath)
            };

            await EmitAsync(new SecurityEvent
            {
                Type = EventType.HONEY_ACCESS,
                Source = EventSource.DECEPTION,
                Severity = EventSeverity.CRITICAL,
                IsHoney = true,
                RiskContribution = 70,
                File = honeyFile,
                User = Actor(),
                Process = new ProcessContext { ProcessId = 4242, ProcessName = "exfil-tool.exe", IsSuspicious = true }
            }, cancellationToken).ConfigureAwait(false);
        }

        await Task.Delay(550, cancellationToken).ConfigureAwait(false);

        await EmitAsync(new SecurityEvent
        {
            Type = EventType.MASS_FILE_ACTIVITY,
            Source = EventSource.FILE_SYSTEM,
            Severity = EventSeverity.HIGH,
            RiskContribution = 45,
            IsProtected = true,
            User = Actor(),
            Process = new ProcessContext { ProcessId = 4242, ProcessName = "exfil-tool.exe", IsSuspicious = true },
            Metadata = { ["filesCopied"] = 128, ["destination"] = "USB" }
        }, cancellationToken).ConfigureAwait(false);

        return Summarize("USB exfiltration");
    }

    private async Task<string> RunInsiderHarvestAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("Attack simulation: insider credential harvest (honey → copy → mass)");

        await _honeyFileService.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var honey = (await _honeyFileService.GetHoneyFilesAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault();

        if (honey is not null)
        {
            var honeyFile = new FileContext
            {
                Path = honey.FilePath,
                IsHoney = true,
                SensitivityLevel = 9,
                Extension = System.IO.Path.GetExtension(honey.FilePath)
            };

            await EmitAsync(new SecurityEvent
            {
                Type = EventType.HONEY_ACCESS,
                Source = EventSource.DECEPTION,
                Severity = EventSeverity.CRITICAL,
                IsHoney = true,
                RiskContribution = 70,
                File = honeyFile,
                User = Actor(),
                Process = new ProcessContext
                {
                    ProcessId = 5151,
                    ProcessName = "powershell.exe",
                    IsSuspicious = true
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);

        var stolenPath = honey?.FilePath ?? @"C:\Users\Public\Documents\stolen.txt";
        var stolenFile = new FileContext
        {
            Path = stolenPath,
            IsHoney = honey is not null,
            SensitivityLevel = 8,
            Extension = System.IO.Path.GetExtension(stolenPath)
        };

        await EmitAsync(new SecurityEvent
        {
            Type = EventType.FILE_COPY,
            Source = EventSource.FILE_SYSTEM,
            Severity = EventSeverity.HIGH,
            RiskContribution = 25,
            IsProtected = true,
            File = stolenFile,
            User = Actor(),
            Process = new ProcessContext { ProcessId = 5151, ProcessName = "powershell.exe", IsSuspicious = true }
        }, cancellationToken).ConfigureAwait(false);

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);

        await EmitAsync(new SecurityEvent
        {
            Type = EventType.MASS_FILE_ACTIVITY,
            Source = EventSource.FILE_SYSTEM,
            Severity = EventSeverity.HIGH,
            RiskContribution = 45,
            IsProtected = true,
            User = Actor(),
            Process = new ProcessContext { ProcessId = 5151, ProcessName = "powershell.exe", IsSuspicious = true },
            Metadata = { ["filesCopied"] = 64, ["destination"] = "TEMP" }
        }, cancellationToken).ConfigureAwait(false);

        return Summarize("Insider credential harvest");
    }

    private Task EmitAsync(SecurityEvent evt, CancellationToken cancellationToken)
        => _threatEngine.ProcessEventAsync(evt, cancellationToken);

    private string Summarize(string scenarioName)
    {
        var state = _threatEngine.GetCurrentState();
        var incident = state.ActiveIncident;
        var message =
            $"{scenarioName} complete. Risk={state.CurrentRisk}, Level={state.Level}, " +
            $"Confidence={state.Confidence:P0}, Action={incident?.Action.ToString() ?? "NONE"}, " +
            $"Incident={incident?.Number ?? "(none)"}";
        _logger.LogWarning("{Message}", message);
        return message;
    }

    private static UserContext Actor() => new()
    {
        UserName = Environment.UserName,
        UserId = Environment.UserName
    };
}
