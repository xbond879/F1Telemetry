using System;
using System.Collections.ObjectModel;
using System.Linq;
using AutoMapper;
using CommunityToolkit.Mvvm.ComponentModel;
using F1TelemetryStorage;
using F1TelemetryStorage.Models;
using F1UdpParser.Models;
using LiveChartsCore.Defaults;
using Microsoft.Extensions.Logging;

namespace F1TelemetryWasm.Models;

public partial class LapData(ILogger<LapData> logger, ITelemetryStorage storage, IMapper mapper) : ObservableObject
{
    public double RaceDistance { get; private set; } = 10000;
    public PacketSessionData SessionData { get; private set; }

    [ObservableProperty]
    private double _currentDistance = -1;

    [ObservableProperty]
    private uint _flashbacksCount = 0;

    [ObservableProperty]
    private uint _currentLap = 0;
    [ObservableProperty]
    private TimeSpan _bestLapTime;
    private uint _bestLapTimeInMsValue = int.MaxValue;
    public uint BestLapTimeInMs
    {
        get => _bestLapTimeInMsValue;
        private set
        {
            BestLapTime = new TimeSpan(0, 0, 0, 0, (int)value);
            _bestLapTimeInMsValue = value;
        }
    }

    public ObservableCollection<ObservablePoint> ThrottleValues { get; } = [];
    public ObservableCollection<ObservablePoint> ThrottleValuesBest { get; } = [];

    public ObservableCollection<ObservablePoint> BrakeValues { get; } = [];
    public ObservableCollection<ObservablePoint> BrakeValuesBest { get; } = [];

    public ObservableCollection<ObservablePoint> SpeedValues { get; } = [];
    public ObservableCollection<ObservablePoint> SpeedValuesBest { get; } = [];

    private void UpdateCollectionValue(ObservableCollection<ObservablePoint> collection, double value)
    {
        if (collection.Count >= 2 &&
            (collection.Last().Y.Equals(value) && collection[^2].Y.Equals(value)))
        {
            collection.Last().X = CurrentDistance;
        }
        else
        {
            collection.Add(new ObservablePoint(CurrentDistance, value));
        }
    }

    private void TruncCollectionValues(ObservableCollection<ObservablePoint> collection, double value)
    {
        var i = collection.Count - 1;
        while (i >= 0 && collection[i].X > value)
        {
            collection.RemoveAt(i);
            i--;
        }
    }

    public bool ApplyUpdate(BasePacketData packet, object sync)
    {
        switch (packet)
        {
            case PacketSessionData sessionData:
                if (!sessionData.SessionEquals(SessionData))
                {
                    var data = storage.Load(sessionData.TrackId, sessionData.SessionType);
                    if (data != null)
                    {
                        logger.LogInformation($"Saved session found. Applying telemetry with time {data.BestLapTimeInMs} ms");
                        mapper.Map(data,this);
                    }
                }

                SessionData = sessionData;
                RaceDistance = sessionData.TrackLength;
                return true;
            
            case PacketCarTelemetryData carTelemetryData:
                if (CurrentDistance < 0)
                    return false;

                lock (sync)
                {
                    UpdateCollectionValue(ThrottleValues, carTelemetryData.Throttle * 100);
                    UpdateCollectionValue(BrakeValues, carTelemetryData.Brake * 100);
                    SpeedValues.Add(new ObservablePoint(CurrentDistance, carTelemetryData.Speed));
                }

                return true;
            case PacketLapData lapData:
                if (lapData.DriverStatus == 0 || lapData.LapDistance < 0)
                    return false;
                if (CurrentLap != lapData.CurrentLapNum)
                {
                    lock (sync)
                    {
                        CurrentLap = lapData.CurrentLapNum;
                        logger.LogInformation($"Lap {lapData.CurrentLapNum} started.");

                        if ((BestLapTimeInMs == 0 || lapData.LastLapTimeInMs < BestLapTimeInMs)
                            && ThrottleValues.Any()
                            && ThrottleValues.First().X < 10
                            && ThrottleValues.Last().X + 10 > RaceDistance)
                        {
                            logger.LogInformation("Last lap was the best! Saving...");
                            BestLapTimeInMs = lapData.LastLapTimeInMs;

                            ThrottleValuesBest.Clear();
                            BrakeValuesBest.Clear();
                            SpeedValuesBest.Clear();

                            foreach (var point in ThrottleValues)
                            {
                                ThrottleValuesBest.Add(new ObservablePoint(point.X, point.Y));
                            }

                            foreach (var point in BrakeValues)
                            {
                                BrakeValuesBest.Add(new ObservablePoint(point.X, point.Y));
                            }

                            foreach (var point in SpeedValues)
                            {
                                SpeedValuesBest.Add(new ObservablePoint(point.X, point.Y));
                            }
                        }

                        ThrottleValues.Clear();
                        BrakeValues.Clear();
                        SpeedValues.Clear();

                        var data = mapper.Map<LapTelemetryData>(this);
                        storage.Save(data);
                    }
                }
                else if (CurrentDistance > lapData.LapDistance)
                {
                    logger.LogInformation($"Flashback from {CurrentDistance} to {lapData.LapDistance}.");
                    lock (sync)
                    {
                        TruncCollectionValues(ThrottleValues, lapData.LapDistance);
                        TruncCollectionValues(BrakeValues, lapData.LapDistance);
                        TruncCollectionValues(SpeedValues, lapData.LapDistance);

                        FlashbacksCount++;
                    }
                }

                CurrentDistance = lapData.LapDistance;
                return true;
            default:
                return false;
        }
    }
}