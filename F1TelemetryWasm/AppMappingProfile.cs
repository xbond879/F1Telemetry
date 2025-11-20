using System.Drawing;
using AutoMapper;
using F1TelemetryStorage.Models;
using F1TelemetryWasm.Models;
using LiveChartsCore.Defaults;

namespace F1TelemetryWasm;

public class AppMappingProfile : Profile
{
    public AppMappingProfile()
    {			
        CreateMap<ObservablePoint, Point>().ReverseMap();
        CreateMap<LapData, LapTelemetryData>()
            .ForMember(data => data.TrackId, opt => opt.MapFrom(ld => ld.SessionData.TrackId))
            .ForMember(data => data.SessionType, opt => opt.MapFrom(ld => ld.SessionData.SessionType));
        
        CreateMap<LapTelemetryData, LapData>();
    }
}