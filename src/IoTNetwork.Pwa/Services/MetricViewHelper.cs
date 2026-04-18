using IoTNetwork.Pwa.Models;

namespace IoTNetwork.Pwa.Services;

public static class MetricViewHelper
{
    public static string Title(TelemetryMetricKind kind) =>
        kind switch
        {
            TelemetryMetricKind.Temperature => "Temperatura",
            TelemetryMetricKind.Humidity => "Humedad relativa",
            TelemetryMetricKind.Co2 => "Dióxido de carbono (CO₂)",
            TelemetryMetricKind.Noise => "Nivel sonoro",
            _ => ""
        };

    public static string MetricKey(TelemetryMetricKind kind) =>
        kind switch
        {
            TelemetryMetricKind.Temperature => "temperature",
            TelemetryMetricKind.Humidity => "humidity",
            TelemetryMetricKind.Co2 => "co2",
            TelemetryMetricKind.Noise => "noise",
            _ => "temperature"
        };

    public static MetricBand Band(TelemetryReadingDto? reading, TelemetryMetricKind kind)
    {
        if (reading is null) return MetricBand.Unknown;
        return kind switch
        {
            TelemetryMetricKind.Temperature => TelemetryLevels.TemperatureBand(reading.Temperature),
            TelemetryMetricKind.Humidity => TelemetryLevels.HumidityBand(reading.Humidity),
            TelemetryMetricKind.Co2 => TelemetryLevels.Co2Band(reading.Co2),
            TelemetryMetricKind.Noise => TelemetryLevels.NoiseBand(reading.NoiseLevel),
            _ => MetricBand.Unknown
        };
    }

    public static string FormatValue(TelemetryReadingDto? reading, TelemetryMetricKind kind) =>
        reading is null
            ? "—"
            : kind switch
            {
                TelemetryMetricKind.Temperature => Format(reading.Temperature, "°C"),
                TelemetryMetricKind.Humidity => Format(reading.Humidity, "%"),
                TelemetryMetricKind.Co2 => Format(reading.Co2, " ppm"),
                TelemetryMetricKind.Noise => Format(reading.NoiseLevel, " dB"),
                _ => "—"
            };

    private static string Format(double? v, string suffix) =>
        v.HasValue ? $"{v.Value:0.#}{suffix}" : "—";
}
