using System.Net.Sockets;
using System.Runtime.CompilerServices;
using F1UdpParser.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace F1UdpParser;

public class F1TelemetryListener(ILogger<F1TelemetryListener> logger, IF1TelemetryConsumer receiver) : BackgroundService
{
    private const int DefaultListenPort = 20777;
    
    public async IAsyncEnumerable<BasePacketData> Start([EnumeratorCancellation] CancellationToken stoppingToken, int listenPort = DefaultListenPort)
    {
        logger.LogInformation($"UDP Server starting on port {listenPort}...");

        var listener = new UdpClient(listenPort);

        logger.LogInformation($"UDP Server started, listening on port {listenPort}...");
        while (true)
        {
            UdpReceiveResult udpData = await listener.ReceiveAsync(stoppingToken);
            if (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("MyBackgroundService is stopping at: {time}", DateTimeOffset.Now);
                listener.Close();
                break;
            }
            var result = await F1PackageParser.ParsePackage(udpData.Buffer);
            if (result is not null)
            {
                yield return result;
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var data in Start(stoppingToken))
        {
            receiver.ReceivePacket(data);
        }
    }
}