using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using MayaJaal.Guardian.Collectors;
using MayaJaal.Guardian.IPC;
using MayaJaal.Infrastructure;
using MayaJaal.Shared.Contracts;

namespace MayaJaal.Guardian;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var runDemo = args.Any(a => string.Equals(a, "--demo", StringComparison.OrdinalIgnoreCase));
        var consoleOnly = args.Any(a => string.Equals(a, "--console", StringComparison.OrdinalIgnoreCase)) || runDemo;

        var logRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "MayaJaal",
            "Logs");
        Directory.CreateDirectory(logRoot);
        EnsureProgramDataLayout();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(logRoot, "guardian-.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddSerilog();

            if (!consoleOnly)
            {
                builder.Services.AddWindowsService(options =>
                {
                    options.ServiceName = "MayaJaal Guardian";
                });
            }

            builder.Services.AddMayaJaalCore();
            builder.Services.AddSingleton<IThreatEngine, ThreatEngine>();
            builder.Services.AddSingleton<DemoScenarioRunner>();
            builder.Services.AddHostedService<IpcServer>();
            builder.Services.AddHostedService(sp =>
            {
                var enableDemoSignals = args.Any(a =>
                    string.Equals(a, "--simulate", StringComparison.OrdinalIgnoreCase));
                return new EventCollector(
                    sp.GetRequiredService<IThreatEngine>(),
                    sp.GetRequiredService<IHoneyFileService>(),
                    sp.GetRequiredService<IAssetProtectionService>(),
                    sp.GetRequiredService<ILogger<EventCollector>>(),
                    enableDemoSignals: enableDemoSignals);
            });
            builder.Services.AddHostedService<UsbCollector>();
            builder.Services.AddHostedService<ProcessCollector>();
            builder.Services.AddHostedService<GuardianBootstrap>();

            var host = builder.Build();

            if (runDemo)
            {
                var eventStore = host.Services.GetRequiredService<IEventStore>();
                await eventStore.InitializeAsync().ConfigureAwait(false);
                var demo = host.Services.GetRequiredService<DemoScenarioRunner>();
                _ = host.StartAsync();
                await Task.Delay(750).ConfigureAwait(false);
                await demo.RunAsync().ConfigureAwait(false);
                await host.StopAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                return 0;
            }

            Log.Information("MayaJaal Guardian starting ({Mode})", consoleOnly ? "console" : "service");
            await host.RunAsync().ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Guardian terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
        }
    }

    private static void EnsureProgramDataLayout()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "MayaJaal");
        foreach (var sub in new[] { "Logs", "Data", "Vaults", "Incidents", "Evidence", "Config" })
            Directory.CreateDirectory(Path.Combine(root, sub));
    }
}

/// <summary>
/// Startup: durable stores, decoys, vault bootstrap, threat replay.
/// </summary>
internal sealed class GuardianBootstrap : IHostedService
{
    private readonly IEventStore _eventStore;
    private readonly IHoneyFileService _honeyFileService;
    private readonly IVaultService _vaultService;
    private readonly IThreatEngine _threatEngine;
    private readonly ILogger<GuardianBootstrap> _logger;

    public GuardianBootstrap(
        IEventStore eventStore,
        IHoneyFileService honeyFileService,
        IVaultService vaultService,
        IThreatEngine threatEngine,
        ILogger<GuardianBootstrap> logger)
    {
        _eventStore = eventStore;
        _honeyFileService = honeyFileService;
        _vaultService = vaultService;
        _threatEngine = threatEngine;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _eventStore.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await _honeyFileService.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await VaultBootstrapper.EnsureDefaultVaultAsync(_vaultService, _logger, cancellationToken)
            .ConfigureAwait(false);
        await _threatEngine.ReplayRecentEventsAsync(500, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Guardian bootstrap complete");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
