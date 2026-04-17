namespace IoTNetwork.Core.Domain.Entities;

/// <summary>
/// Calendar day (UTC) for which a node has telemetry; supports fast "available dates" queries.
/// </summary>
public sealed class NodeDataDay
{
    public string NodeId { get; set; } = string.Empty;

    public DateOnly DayUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
