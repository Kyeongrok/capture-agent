using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CaptureAgent.Forms;
using CaptureAgent.Forms.UI.Views;
using CaptureAgent.Forms.ViewModels;
using CaptureAgent.Main.PInvoke;
using CaptureAgent.Main.Services;

namespace CaptureAgent;

public class App : Application
{
    private IHost? _host;

    public static IServiceProvider? ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            })
            .Build();

        ServiceProvider = _host.Services;
        AppServices.Current = _host.Services;

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Phase 1: Core Infrastructure (PInvoke & Services)
        services.AddSingleton<IScreenMetricsService, ScreenMetricsService>();
        services.AddSingleton<IMouseInteropService, MouseInteropService>();
        services.AddSingleton<IScreenCaptureService, ScreenCaptureService>();
        services.AddSingleton<IImageSaveService, ImageSaveService>();
        services.AddSingleton<IHistoryService, HistoryService>();
        services.AddSingleton<IMacroService, MacroService>();

        // ViewModels
        services.AddSingleton<RegionViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
    }
}
