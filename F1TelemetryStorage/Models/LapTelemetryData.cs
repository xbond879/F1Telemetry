using System.Drawing;

namespace F1TelemetryStorage.Models;

public record LapTelemetryData()
{
    public sbyte TrackId { get; init; }
    public byte SessionType { get; init; }
    public uint BestLapTimeInMs { get; init; }
    public List<Point> ThrottleValuesBest { get; init; }
    public List<Point> BrakeValuesBest { get; init; }
    public List<Point> SpeedValuesBest { get; init; }
}