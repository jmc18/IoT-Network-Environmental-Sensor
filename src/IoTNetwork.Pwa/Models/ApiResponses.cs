using System.Text.Json.Serialization;

namespace IoTNetwork.Pwa.Models;

public sealed class NodesListDto
{
    [JsonPropertyName("nodes")]
    public List<string> Nodes { get; set; } = [];
}

public sealed class TelemetryReadingDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("nodeId")]
    public string NodeId { get; set; } = string.Empty;

    [JsonPropertyName("timestampUtc")]
    public DateTime TimestampUtc { get; set; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("humidity")]
    public double? Humidity { get; set; }

    [JsonPropertyName("co2")]
    public double? Co2 { get; set; }

    [JsonPropertyName("noiseLevel")]
    public double? NoiseLevel { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }
}

public sealed class PagedReadingsDto
{
    [JsonPropertyName("items")]
    public List<TelemetryReadingDto> Items { get; set; } = [];
}

public sealed class AvailableDatesDto
{
    [JsonPropertyName("dates")]
    public List<string> Dates { get; set; } = [];
}
