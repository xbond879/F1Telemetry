using F1TelemetryOverlay.Models;
using F1TelemetryStorage;
using F1UdpParser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace F1TelemetryOverlay;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        using var host = CreateHostBuilder(args).Build();
        await host.StartAsync();
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

        host.Services.GetRequiredService<TelemetryRenderer>();
        
        Console.ReadLine();
        
        lifetime.StopApplication();
        await host.WaitForShutdownAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseConsoleLifetime()
            .ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Information))
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<F1TelemetryListener>();
                services.AddAutoMapper(cfg => cfg.AddProfile<OverlayMappingProfile>());
                services.AddSingleton<TelemetryRenderer>();
                services.AddSingleton<IF1TelemetryConsumer, LiveTelemetryConsumer>();
                services.AddSingleton<ITelemetryStorage, LiteDbStorage>();
                services.AddSingleton<ActiveTelemetryData>();
            });
}
