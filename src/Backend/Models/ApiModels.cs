using System.Text.Json.Serialization;

namespace Backend.Models;

public sealed class TelemetryIngestRequest
{
    [JsonPropertyName("timestampUtc")]
    public DateTime? TimestampUtc { get; init; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; init; }

    [JsonPropertyName("humidity")]
    public double? Humidity { get; init; }

    [JsonPropertyName("co2")]
    public double? Co2 { get; init; }

    [JsonPropertyName("noiseLevel")]
    public double? NoiseLevel { get; init; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; init; }
}

public sealed class TelemetryReadingView
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("nodeId")]
    public string NodeId { get; init; } = string.Empty;

    [JsonPropertyName("timestampUtc")]
    public DateTime TimestampUtc { get; init; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; init; }

    [JsonPropertyName("humidity")]
    public double? Humidity { get; init; }

    [JsonPropertyName("co2")]
    public double? Co2 { get; init; }

    [JsonPropertyName("noiseLevel")]
    public double? NoiseLevel { get; init; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; init; }

    public static TelemetryReadingView FromDocument(TelemetryDocument d) =>
        new()
        {
            Id = d.Id,
            NodeId = d.NodeId,
            TimestampUtc = d.TimestampUtc,
            Temperature = d.Temperature,
            Humidity = d.Humidity,
            Co2 = d.Co2,
            NoiseLevel = d.NoiseLevel,
            Latitude = d.Latitude,
            Longitude = d.Longitude,
        };
}

public sealed class PagedTelemetryResponse
{
    [JsonPropertyName("items")]
    public IReadOnlyList<TelemetryReadingView> Items { get; init; } = Array.Empty<TelemetryReadingView>();

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; init; }
}

public sealed class AvailableDatesResponse
{
    [JsonPropertyName("dates")]
    public IReadOnlyList<string> Dates { get; init; } = Array.Empty<string>();
}

public sealed class NodesListResponse
{
    [JsonPropertyName("nodes")]
    public IReadOnlyList<string> Nodes { get; init; } = Array.Empty<string>();
}

public sealed class TelemetryUpdatedPayload
{
    [JsonPropertyName("nodeId")]
    public string NodeId { get; init; } = string.Empty;

    [JsonPropertyName("readingId")]
    public string ReadingId { get; init; } = string.Empty;

    [JsonPropertyName("timestampUtc")]
    public DateTime TimestampUtc { get; init; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; init; }

    [JsonPropertyName("humidity")]
    public double? Humidity { get; init; }

    [JsonPropertyName("co2")]
    public double? Co2 { get; init; }

    [JsonPropertyName("noiseLevel")]
    public double? NoiseLevel { get; init; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; init; }
}
