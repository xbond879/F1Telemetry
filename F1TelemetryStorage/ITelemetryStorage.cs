using F1TelemetryStorage.Models;

namespace F1TelemetryStorage;

public interface ITelemetryStorage
{
    void Save(LapTelemetryData data);
    LapTelemetryData? Load(sbyte trackId, byte sessionDataSessionType);
}