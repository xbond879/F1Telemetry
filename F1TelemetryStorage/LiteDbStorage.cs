using F1TelemetryStorage.Models;
using LiteDB;

namespace F1TelemetryStorage;

public class LiteDbStorage : ITelemetryStorage
{
    private const string DbPath = @"F1TelemetryData.db";

    public void Save(LapTelemetryData data)
    {
        using var db = new LiteDatabase(DbPath);

        var col = db.GetCollection<LapTelemetryData>();
        col.EnsureIndex(x => x.TrackId, true);

        col.DeleteMany(x => x.TrackId == data.TrackId);
        col.Insert(data);
    }
    
    public LapTelemetryData? Load(sbyte trackId, byte sessionType)
    {
        using var db = new LiteDatabase(DbPath);
        var col = db.GetCollection<LapTelemetryData>();
        return col
            .Find(t => t.TrackId == trackId && t.SessionType == sessionType)
            .OrderByDescending(t => t.BestLapTimeInMs)
            .FirstOrDefault();
    }
}