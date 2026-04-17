namespace Backend.Infrastructure;

internal static class SignalRConstants
{
    public const string HubName = "telemetry";

    /// <summary>
    /// Application setting that holds the Azure SignalR Service connection string.
    /// </summary>
    public const string ConnectionSetting = "AzureSignalRConnectionString";

    public const string TelemetryUpdatedTarget = "telemetryUpdated";
}
