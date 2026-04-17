using Newtonsoft.Json;

namespace Backend.Models;

/// <summary>
/// One document per node per UTC calendar day that had telemetry. Partition key is <see cref="NodeId"/>.
/// Document id is yyyy-MM-dd.
/// </summary>
public sealed class NodeDayIndexDocument
{
    [JsonProperty("id")]
    public string Id { get; init; } = string.Empty;

    [JsonProperty("nodeId")]
    public string NodeId { get; init; } = string.Empty;

    [JsonProperty("dayUtc")]
    public string DayUtc { get; init; } = string.Empty;

    [JsonProperty("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; init; }
}
