using IoTNetwork.Core.Application.Dtos;
using IoTNetwork.Core.Domain.Entities;
using Mapster;

namespace IoTNetwork.Api.Mappings;

public static class MappingConfig
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig<TelemetryReading, TelemetryReadingDto>.NewConfig();

        TypeAdapterConfig<TelemetryIngestDto, TelemetryReading>.NewConfig()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.NodeId)
            .Ignore(dest => dest.TimestampUtc);
    }
}
