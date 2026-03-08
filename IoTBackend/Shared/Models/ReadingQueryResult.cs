using System.Text.Json.Serialization;

namespace IoTBackend.Shared.Models;

/// <summary>
/// Result DTO for GET /api/readings
/// </summary>
public class ReadingQueryResult
{
    [JsonPropertyName("readings")]
    public IReadOnlyList<NodeDocument> Readings { get; set; } = [];

    [JsonPropertyName("count")]
    public int Count => Readings.Count;

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }
}
