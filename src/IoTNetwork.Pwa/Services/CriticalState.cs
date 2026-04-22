using IoTNetwork.Pwa.Models;

namespace IoTNetwork.Pwa.Services;

/// <summary>
/// Evalúa si una lectura tiene al menos una métrica en banda High
/// (Temp, CO2 o Ruido) y por lo tanto se considera "crítica" para humanos.
/// </summary>
public static class CriticalState
{
    public static bool IsCritical(TelemetryReadingDto? reading) =>
        CriticalMetrics(reading).Count > 0;

    public static IReadOnlyList<string> CriticalMetrics(TelemetryReadingDto? reading)
    {
        if (reading is null) return Array.Empty<string>();
        var list = new List<string>(3);
        if (TelemetryLevels.TemperatureBand(reading.Temperature) == MetricBand.High)
            list.Add("Temperatura");
        if (TelemetryLevels.Co2Band(reading.Co2) == MetricBand.High)
            list.Add("CO₂");
        if (TelemetryLevels.NoiseBand(reading.NoiseLevel) == MetricBand.High)
            list.Add("Ruido");
        return list;
    }
}
