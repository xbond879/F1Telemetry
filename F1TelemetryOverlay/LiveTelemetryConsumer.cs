using System.Collections.Immutable;
using AutoMapper;
using F1TelemetryOverlay.Models;
using F1TelemetryStorage;
using F1TelemetryStorage.Models;
using F1UdpParser;
using F1UdpParser.Models;
using Microsoft.Extensions.Logging;

namespace F1TelemetryOverlay;

public class LiveTelemetryConsumer(ILogger<LiveTelemetryConsumer> logger, IMapper mapper, ActiveTelemetryData data, ITelemetryStorage storage) : IF1TelemetryConsumer
{
    private uint _currentDistance;
    
    public void ReceivePacket(BasePacketData packet)
    {
        switch (packet)
        {
            case PacketSessionData sessionData:
                if (!sessionData.SessionEquals(data.SessionData))
                {
                    logger.LogInformation($"Track change detected...");
                    var loadedData = storage.Load(sessionData.TrackId, sessionData.SessionType);
                    if (loadedData != null)
                    {
                        logger.LogInformation($"Saved session found. Applying telemetry with time {loadedData.BestLapTimeInMs} ms");
                        lock (data)
                        {
                            mapper.Map(loadedData, data);
                        }
                    }
                }

                data.SessionData = sessionData;
                return;

            case PacketCarTelemetryData telemetryData:
                //logger.LogInformation($"{currentDistance:F4}\t{telemetryData.Throttle:F4}\t{telemetryData.Brake:F4}\t{telemetryData.Steer:F4} ");
                lock (data)
                {
                    data.CurrentSpeedValues[_currentDistance] = telemetryData.Speed;
                    data.CurrentThrottleValues[_currentDistance] = telemetryData.Throttle;
                    data.CurrentBrakeValues[_currentDistance] = telemetryData.Brake;
                }

                break;
            case PacketLapData lapData:
                if (lapData.DriverStatus == 0 || lapData.LapDistance < 0)
                    return;
                lock (data)
                {
                    if (data.CurrentLapNum != lapData.CurrentLapNum)
                    {
                        data.CurrentLapNum = lapData.CurrentLapNum;
                        logger.LogInformation($"Lap {lapData.CurrentLapNum} started.");

                        //TODO: refactor in a method
                        if ((data.BestLapTime == 0 || lapData.LastLapTimeInMs < data.BestLapTime)
                            && data.CurrentThrottleValues.Any()
                            && data.CurrentThrottleValues.First().Key < 10
                            && data.CurrentThrottleValues.Last().Key + 10 > data.SessionData?.TrackLength)
                        {
                            logger.LogInformation($"Last lap was the best ({lapData.LastLapTimeInMs}ms)! Saving... (Previous: {data.BestLapTime}ms)");
                            data.BestLapTime = lapData.LastLapTimeInMs;

                            data.BestLapThrottleValues = data.CurrentThrottleValues.ToImmutableSortedDictionary();
                            data.BestLapBrakeValues = data.CurrentBrakeValues.ToImmutableSortedDictionary();
                            data.BestLapSpeedValues = data.CurrentSpeedValues.ToImmutableSortedDictionary();

                            var saveData = mapper.Map<LapTelemetryData>(data);
                            storage.Save(saveData);
                        }

                        data.CurrentThrottleValues = new();
                        data.CurrentBrakeValues = new();
                        data.CurrentSpeedValues = new();
                    }
                    else if (_currentDistance > lapData.LapDistance)
                    {
                        logger.LogInformation($"Flashback from {_currentDistance} to {lapData.LapDistance}.");
                        data.CurrentThrottleValues =
                            new(data.CurrentThrottleValues.Where(t => t.Key <= lapData.LapDistance));
                        data.CurrentBrakeValues =
                            new(data.CurrentBrakeValues.Where(t => t.Key <= lapData.LapDistance));
                        data.CurrentSpeedValues =
                            new(data.CurrentSpeedValues.Where(t => t.Key <= lapData.LapDistance));
                    }

                    _currentDistance = (uint)lapData.LapDistance;
                    data.CurrentLapNum = lapData.CurrentLapNum;
                }

                //logger.LogInformation($"[{DateTime.Now.Minute}.{DateTime.Now.Second}.{DateTime.Now.Millisecond}] Received {JsonSerializer.Serialize(lapData, jsonSerializerOptions)}");
                //logger.LogInformation($"lapDistance: {lapData.m_lapDistance} ");
                break;
        }
    }
}