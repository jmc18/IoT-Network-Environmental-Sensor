namespace IoTNetwork.Core.Domain.Entities;

/// <summary>
/// One environmental reading from an ESP32 node (stored in PostgreSQL).
/// </summary>
public sealed class TelemetryReading
{
    public Guid Id { get; set; }

    public string NodeId { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; }

    public double? Temperature { get; set; }

    public double? Humidity { get; set; }

    public double? Co2 { get; set; }

    public double? NoiseLevel { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}
