using IoTBackend.Shared.Models;
using IoTBackend.Shared.Services;

namespace Functions.Services;

/// <summary>
/// Throws when Cosmos DB is not configured. Used so DI resolves but operations fail with clear message.
/// </summary>
public class NullCosmosDbService : ICosmosDbService
{
    public Task<NodeDocument> CreateReadingAsync(NodeDocument document, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Cosmos DB is not configured. Set CosmosDb__ConnectionString or COSMOS_CONNECTION_STRING.");

    public Task<(IReadOnlyList<NodeDocument> Items, string? ContinuationToken)> QueryReadingsAsync(ReadingQueryParams parameters, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Cosmos DB is not configured. Set CosmosDb__ConnectionString or COSMOS_CONNECTION_STRING.");

    public Task<IReadOnlyList<NodeDocument>> GetLatestReadingsAsync(string? nodeId, double? lat, double? lon, double? latDelta, double? lonDelta, int maxCount = 50, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Cosmos DB is not configured. Set CosmosDb__ConnectionString or COSMOS_CONNECTION_STRING.");
}
