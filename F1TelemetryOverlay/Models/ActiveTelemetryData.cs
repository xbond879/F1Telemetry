using System.Collections.Immutable;
using F1UdpParser.Models;

namespace F1TelemetryOverlay.Models;

public class ActiveTelemetryData
{
    public PacketSessionData? SessionData { get; set; }

    public int CurrentLapNum { get; set; }
    public Dictionary<uint, float> CurrentThrottleValues { get; set; } = new();
    public Dictionary<uint, float> CurrentBrakeValues { get; set; } = new();
    public Dictionary<uint, uint> CurrentSpeedValues { get; set; } = new();

    public uint BestLapTime { get; set; } = 0;
    public ImmutableSortedDictionary<uint, float> BestLapThrottleValues { get; set; }
    public ImmutableSortedDictionary<uint, float> BestLapBrakeValues { get; set; }
    public ImmutableSortedDictionary<uint, uint> BestLapSpeedValues { get; set; }
}