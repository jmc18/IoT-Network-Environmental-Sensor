namespace IoTNetwork.Pwa.Services;

public enum MetricBand
{
    Unknown,
    Low,
    Medium,
    High
}

/// <summary>
/// Umbrales orientativos para aulas / interiores escolares (lectura rápida en campo).
/// </summary>
public static class TelemetryLevels
{
    public static MetricBand TemperatureBand(double? celsius)
    {
        if (celsius is null) return MetricBand.Unknown;
        var v = celsius.Value;
        if (v < 19) return MetricBand.Low;
        if (v <= 26) return MetricBand.Medium;
        return MetricBand.High;
    }

    public static MetricBand HumidityBand(double? rhPercent)
    {
        if (rhPercent is null) return MetricBand.Unknown;
        var v = rhPercent.Value;
        if (v < 35) return MetricBand.Low;
        if (v <= 65) return MetricBand.Medium;
        return MetricBand.High;
    }

    public static MetricBand Co2Band(double? ppm)
    {
        if (ppm is null) return MetricBand.Unknown;
        var v = ppm.Value;
        if (v < 800) return MetricBand.Low;
        if (v <= 1200) return MetricBand.Medium;
        return MetricBand.High;
    }

    public static MetricBand NoiseBand(double? dB)
    {
        if (dB is null) return MetricBand.Unknown;
        var v = dB.Value;
        if (v < 40) return MetricBand.Low;
        if (v <= 60) return MetricBand.Medium;
        return MetricBand.High;
    }

    public static string BandLabelEs(MetricBand band) =>
        band switch
        {
            MetricBand.Low => "Baja",
            MetricBand.Medium => "Media",
            MetricBand.High => "Alta",
            _ => "Sin dato"
        };

    public static string BandDescriptionEs(string metricName, MetricBand band) =>
        (metricName, band) switch
        {
            ("temperature", MetricBand.Low) => "Por debajo del rango de confort habitual en aula (referencia: menos de 19 °C).",
            ("temperature", MetricBand.Medium) => "Dentro de un rango de confort típico para interiores (aprox. 19 a 26 °C).",
            ("temperature", MetricBand.High) => "Por encima del rango de confort habitual (más de 26 °C); revisar ventilación o climatización.",
            ("humidity", MetricBand.Low) => "Humedad relativa baja (menos de 35 %); puede causar sequedad ambiental.",
            ("humidity", MetricBand.Medium) => "Humedad relativa en rango moderado (aprox. 35 a 65 %).",
            ("humidity", MetricBand.High) => "Humedad relativa alta (más de 65 %); puede favorecer condensación o malestar.",
            ("co2", MetricBand.Low) => "Concentración de CO₂ baja (menos de 800 ppm); renovación de aire adecuada.",
            ("co2", MetricBand.Medium) => "CO₂ en rango intermedio (aprox. 800 a 1200 ppm); conviene ventilar si se mantiene mucho tiempo.",
            ("co2", MetricBand.High) => "CO₂ elevado (más de 1200 ppm); se recomienda ventilar el espacio.",
            ("noise", MetricBand.Low) => "Nivel sonoro bajo (menos de 40 dB); ambiente silencioso.",
            ("noise", MetricBand.Medium) => "Nivel sonoro moderado (aprox. 40 a 60 dB); típico de aula con actividad.",
            ("noise", MetricBand.High) => "Nivel sonoro alto (más de 60 dB); puede afectar concentración.",
            _ => "No hay lectura disponible para este indicador."
        };

    public static string BandBadgeTailwind(MetricBand band) =>
        band switch
        {
            MetricBand.Low =>
                "bg-emerald-100 text-emerald-900 ring-1 ring-emerald-200 dark:bg-emerald-500/20 dark:text-emerald-100 dark:ring-emerald-500/40",
            MetricBand.Medium =>
                "bg-amber-100 text-amber-900 ring-1 ring-amber-200 dark:bg-amber-500/20 dark:text-amber-100 dark:ring-amber-500/40",
            MetricBand.High =>
                "bg-rose-100 text-rose-900 ring-1 ring-rose-200 dark:bg-rose-500/20 dark:text-rose-100 dark:ring-rose-500/40",
            _ =>
                "bg-slate-200 text-slate-700 ring-1 ring-slate-300 dark:bg-slate-700 dark:text-slate-100 dark:ring-slate-600"
        };

    public static string BandCardAccent(MetricBand band) =>
        band switch
        {
            MetricBand.Low => "border-l-emerald-500",
            MetricBand.Medium => "border-l-amber-500",
            MetricBand.High => "border-l-rose-500",
            _ => "border-l-slate-300 dark:border-l-slate-600"
        };
}
