namespace Backend.Configuration;

public sealed class CosmosOptions
{
    public const string SectionName = "Cosmos";

    public string ConnectionString { get; init; } = string.Empty;

    public string DatabaseName { get; init; } = "iot-network";

    public string TelemetryContainerName { get; init; } = "telemetry";

    public string NodeDataIndexContainerName { get; init; } = "nodeDataIndex";
}

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Comma-separated list of allowed origins for browser clients and SignalR.
    /// </summary>
    public string AllowedOrigins { get; init; } = string.Empty;
}
