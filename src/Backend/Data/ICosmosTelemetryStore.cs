using Backend.Models;

namespace Backend.Data;

public interface ICosmosTelemetryStore
{
    Task<TelemetryDocument> IngestAsync(string nodeId, TelemetryIngestRequest request, CancellationToken cancellationToken);

    Task<PagedTelemetryResponse> GetReadingsAsync(
        string nodeId,
        DateTime fromUtc,
        DateTime toUtc,
        int maxItems,
        string? continuationToken,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetAvailableDatesAsync(string nodeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListNodesAsync(CancellationToken cancellationToken);
}
