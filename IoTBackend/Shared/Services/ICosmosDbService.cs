using IoTBackend.Shared.Models;

namespace IoTBackend.Shared.Services;

public class ReadingQueryParams
{
    public string? NodeId { get; set; }
    public string? Sensor { get; set; }
    public double? Lat { get; set; }
    public double? Lon { get; set; }
    public double? LatDelta { get; set; }
    public double? LonDelta { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int? MaxCount { get; set; } = 100;
    public string? ContinuationToken { get; set; }
}

public interface ICosmosDbService
{
    Task<NodeDocument> CreateReadingAsync(NodeDocument document, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<NodeDocument> Items, string? ContinuationToken)> QueryReadingsAsync(ReadingQueryParams parameters, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NodeDocument>> GetLatestReadingsAsync(string? nodeId, double? lat, double? lon, double? latDelta, double? lonDelta, int maxCount = 50, CancellationToken cancellationToken = default);
}
