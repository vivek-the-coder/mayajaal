using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MayaJaal.SecurityCenter.Services;
using MayaJaal.SecurityCenter.ViewModels;

namespace MayaJaal.SecurityCenter;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private GuardianProcessHost? _guardianHost;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _guardianHost = new GuardianProcessHost();
        try
        {
            await _guardianHost.EnsureRunningAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not start Guardian in the background:\n\n{ex.Message}\n\n" +
                "Security Center will open anyway. Build the solution and retry if IPC stays offline.",
                "MayaJaal",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(_ => _guardianHost);
                services.AddSingleton(_ => new GuardianStatusService());
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        var window = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(2));
            _host.Dispose();
        }

        _guardianHost?.Dispose();
        base.OnExit(e);
    }
}
