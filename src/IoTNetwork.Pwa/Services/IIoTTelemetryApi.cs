using IoTNetwork.Pwa.Models;

namespace IoTNetwork.Pwa.Services;

public interface IIoTTelemetryApi
{
    Task<IReadOnlyList<string>> GetNodesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelemetryReadingDto>> GetReadingsAsync(
        string nodeId,
        DateTime fromUtc,
        DateTime toUtc,
        int maxItems,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetAvailableDatesAsync(string nodeId, CancellationToken cancellationToken = default);

    Task<TelemetryReadingDto?> IngestReadingAsync(
        string nodeId,
        TelemetryIngestDto reading,
        CancellationToken cancellationToken = default);
}
