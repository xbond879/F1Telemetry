using Avalonia;
using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using F1TelemetryWasm;
using F1TelemetryWasm.Models;
using F1TelemetryWasm.ViewModels;
using F1UdpParser;

namespace F1TelemetryDesktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .AfterSetup(_ =>
            {
                var host = Host.CreateDefaultBuilder()
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.AddHostedService<F1TelemetryListener>();
                        services.AddSingleton<IF1TelemetryConsumer, LiveViewModel>();
                        services.AddSingleton<LapData>();
                        services.AddSingleton<LiveViewConfig>();
                        services.AddSingleton<DataUpdateSyncRoot>();
                    })
                    .Build();

                (App.Current as App).ServiceProvider = host.Services;
                host.Start();
            });
    }
}