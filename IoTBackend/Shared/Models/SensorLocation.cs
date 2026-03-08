using System.Text.Json.Serialization;

namespace IoTBackend.Shared.Models;

public class SensorLocation
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }
}
