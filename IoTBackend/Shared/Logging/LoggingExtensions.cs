using Microsoft.Extensions.Logging;

namespace IoTBackend.Shared.Logging;

/// <summary>
/// Structured logging extensions for IoT ingest and query operations.
/// Properties (NodeId, Lat, Lon, etc.) are available in Application Insights for correlation.
/// </summary>
public static class LoggingExtensions
{
    public static void LogIngestRequest(this ILogger logger, string nodeId, double? lat, double? lon)
    {
        logger.LogInformation(
            "Ingest request received for node {NodeId} at location ({Lat}, {Lon})",
            nodeId, lat, lon);
    }

    public static void LogQueryRequest(this ILogger logger, string? nodeId, string? sensor, double? lat, double? lon, int resultCount, long elapsedMs)
    {
        logger.LogInformation(
            "Query completed: NodeId={NodeId}, Sensor={Sensor}, Lat={Lat}, Lon={Lon}, ResultCount={Count}, ElapsedMs={ElapsedMs}",
            nodeId, sensor, lat, lon, resultCount, elapsedMs);
    }

    public static void LogSignalRPublish(this ILogger logger, string nodeId)
    {
        logger.LogInformation("SignalR: Published new reading for node {NodeId}", nodeId);
    }
}
