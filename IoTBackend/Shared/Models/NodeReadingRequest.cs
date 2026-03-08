using System.Text.Json.Serialization;

namespace IoTBackend.Shared.Models;

/// <summary>
/// Request DTO for POST /api/ingest
/// </summary>
public class NodeReadingRequest
{
    [JsonPropertyName("nodeId")]
    public string NodeId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; set; }

    [JsonPropertyName("lat")]
    public double? Lat { get; set; }

    [JsonPropertyName("lon")]
    public double? Lon { get; set; }

    [JsonPropertyName("noise")]
    public double? Noise { get; set; }

    [JsonPropertyName("co2")]
    public double? Co2 { get; set; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("humidity")]
    public double? Humidity { get; set; }
}
