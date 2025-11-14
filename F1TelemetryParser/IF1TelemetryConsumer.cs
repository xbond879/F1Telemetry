using F1UdpParser.Models;

namespace F1UdpParser;

public interface IF1TelemetryConsumer
{
    void ReceivePacket(BasePacketData packet);
}