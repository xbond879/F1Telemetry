using System.Collections.Immutable;
using System.Drawing;
using AutoMapper;
using F1TelemetryOverlay.Models;
using F1TelemetryStorage.Models;
using F1UdpParser.Models;

namespace F1TelemetryOverlay;

public class OverlayMappingProfile : Profile
{
    public OverlayMappingProfile()
    {
        CreateMap<LapTelemetryData, PacketSessionData>();

        CreateMap<List<Point>, ImmutableSortedDictionary<uint, uint>>().ConvertUsing(l => l.ToImmutableSortedDictionary(item => (uint)item.X, item => (uint)item.Y));
        CreateMap<List<Point>, ImmutableSortedDictionary<uint, float>>().ConvertUsing(l => l.ToImmutableSortedDictionary(item => (uint)item.X, item => (float)item.Y));

        CreateMap<LapTelemetryData, ActiveTelemetryData>()
            .ForMember(data => data.BestLapSpeedValues, opt => opt.MapFrom(lp => lp.SpeedValuesBest))
            .ForMember(data => data.BestLapBrakeValues, opt => opt.MapFrom(ld => ld.BrakeValuesBest))
            .ForMember(data => data.BestLapThrottleValues, opt => opt.MapFrom(ld => ld.ThrottleValuesBest))
            .ForMember(data => data.BestLapTime, opt => opt.MapFrom(ld => ld.BestLapTimeInMs))
            .ForMember(data => data.SessionData, opt => opt.MapFrom(ld => ld))
            ;

        CreateMap<KeyValuePair<uint, float>, Point>()
            .ForMember(p => p.X, opt => opt.MapFrom(p => p.Key))
            .ForMember(p => p.Y, opt => opt.MapFrom(p => p.Value))
            ;

        CreateMap<KeyValuePair<uint, uint>, Point>()
            .ForMember(p => p.X, opt => opt.MapFrom(p => p.Key))
            .ForMember(p => p.Y, opt => opt.MapFrom(p => p.Value))
            ;

        CreateMap<ActiveTelemetryData, LapTelemetryData>()
               .ForMember(data => data.TrackId, opt => opt.MapFrom(ld => ld.SessionData.TrackId))
               .ForMember(data => data.SessionType, opt => opt.MapFrom(ld => ld.SessionData.SessionType))
               .ForMember(data => data.BestLapTimeInMs, opt => opt.MapFrom(ld => ld.BestLapTime))
               .ForMember(data => data.SpeedValuesBest, opt => opt.MapFrom(data => data.BestLapSpeedValues))
               .ForMember(data => data.BrakeValuesBest, opt => opt.MapFrom(ld => ld.BestLapBrakeValues))
               .ForMember(data => data.ThrottleValuesBest, opt => opt.MapFrom(ld => ld.BestLapThrottleValues))
               ;
    }
}