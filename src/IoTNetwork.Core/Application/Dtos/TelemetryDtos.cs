namespace IoTNetwork.Core.Application.Dtos;

public sealed class TelemetryReadingDto
{
    public Guid Id { get; init; }

    public string NodeId { get; init; } = string.Empty;

    public DateTime TimestampUtc { get; init; }

    public double? Temperature { get; init; }

    public double? Humidity { get; init; }

    public double? Co2 { get; init; }

    public double? NoiseLevel { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}

public sealed class TelemetryIngestDto
{
    public DateTime? TimestampUtc { get; init; }

    public double? Temperature { get; init; }

    public double? Humidity { get; init; }

    public double? Co2 { get; init; }

    public double? NoiseLevel { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}

public sealed class PagedReadingsDto
{
    public IReadOnlyList<TelemetryReadingDto> Items { get; init; } = Array.Empty<TelemetryReadingDto>();
}

public sealed class AvailableDatesDto
{
    public IReadOnlyList<string> Dates { get; init; } = Array.Empty<string>();
}

public sealed class NodesListDto
{
    public IReadOnlyList<string> Nodes { get; init; } = Array.Empty<string>();
}

public sealed class DeviceTokenRegisterDto
{
    public string Token { get; init; } = string.Empty;

    public string? NodeFilter { get; init; }
}

public sealed class DeviceTokenUnregisterDto
{
    public string Token { get; init; } = string.Empty;
}
