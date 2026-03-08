namespace IoTBackend.Shared.Models;

/// <summary>
/// Enumeration of supported sensor types for filtering
/// </summary>
public static class SensorType
{
    public const string Noise = "noise";
    public const string Co2 = "co2";
    public const string Temperature = "temperature";
    public const string Humidity = "humidity";

    public static readonly string[] All = [Noise, Co2, Temperature, Humidity];
}
