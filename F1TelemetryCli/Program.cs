using System.Text.Json;
using Avalonia.Logging;
using F1UdpParser;
using F1UdpParser.Models;
using Microsoft.Extensions.Logging;

namespace F1TelemetryCli;

public class UdpParser
{
    public static async Task Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            // Optional: Configure minimum log level for console
            builder.SetMinimumLevel(LogLevel.Information); 
        });

        float currentDistance = 0;
        using var listener = new F1TelemetryListener(loggerFactory.CreateLogger<F1TelemetryListener>(), null);
        await foreach (var data in listener.Start(CancellationToken.None))
        {
            switch (data)
            {
                case PacketCarTelemetryData telemetryData:
                    Console.WriteLine(
                        $"{currentDistance:F4}\t{telemetryData.Throttle:F4}\t{telemetryData.Brake:F4}\t{telemetryData.Steer:F4} ");
                    break;
                case PacketLapData lapData:
                    currentDistance = lapData.LapDistance;

                    //Console.WriteLine($"[{DateTime.Now.Minute}.{DateTime.Now.Second}.{DateTime.Now.Millisecond}] Received {JsonSerializer.Serialize(lapData, jsonSerializerOptions)}");
                    //Console.WriteLine($"lapDistance: {lapData.m_lapDistance} ");
                    break;
            }
        }
    }
}