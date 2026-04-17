using Newtonsoft.Json;

namespace Backend.Models;

/// <summary>
/// Stored telemetry reading. Partition key is <see cref="NodeId"/>.
/// </summary>
public sealed class TelemetryDocument
{
    [JsonProperty("id")]
    public string Id { get; init; } = string.Empty;

    [JsonProperty("nodeId")]
    public string NodeId { get; init; } = string.Empty;

    [JsonProperty("timestampUtc")]
    public DateTime TimestampUtc { get; init; }

    [JsonProperty("temperature")]
    public double? Temperature { get; init; }

    [JsonProperty("humidity")]
    public double? Humidity { get; init; }

    [JsonProperty("co2")]
    public double? Co2 { get; init; }

    [JsonProperty("noiseLevel")]
    public double? NoiseLevel { get; init; }

    [JsonProperty("latitude")]
    public double? Latitude { get; init; }

    [JsonProperty("longitude")]
    public double? Longitude { get; init; }
}
