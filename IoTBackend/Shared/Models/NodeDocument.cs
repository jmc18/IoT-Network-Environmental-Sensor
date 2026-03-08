using System.Text.Json.Serialization;

namespace IoTBackend.Shared.Models;

/// <summary>
/// Document stored in Cosmos DB. One document per reading from a node.
/// Partition key: nodeId
/// </summary>
public class NodeDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("nodeId")]
    public string NodeId { get; set; } = string.Empty;

    [JsonPropertyName("partitionKey")]
    public string PartitionKey => NodeId;

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("location")]
    public SensorLocation? Location { get; set; }

    [JsonPropertyName("noise")]
    public double? Noise { get; set; }

    [JsonPropertyName("co2")]
    public double? Co2 { get; set; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("humidity")]
    public double? Humidity { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "SensorReading";
}
