using IoTBackend.Shared.Models;
using IoTBackend.Shared.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Functions.Services;

public class CosmosDbService : ICosmosDbService
{
    private const string DatabaseId = "iot-sensors";
    private const string ContainerId = "readings";

    private readonly Container _container;
    private readonly ILogger<CosmosDbService> _logger;

    public CosmosDbService(CosmosClient cosmosClient, ILogger<CosmosDbService> logger)
    {
        _container = cosmosClient.GetContainer(DatabaseId, ContainerId);
        _logger = logger;
    }

    public async Task<NodeDocument> CreateReadingAsync(NodeDocument document, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(document.Id))
            document.Id = $"{document.NodeId}_{document.Timestamp:O}";

        var response = await _container.CreateItemAsync(document, new PartitionKey(document.NodeId), cancellationToken: cancellationToken);
        _logger.LogDebug("Created reading {Id} for node {NodeId}", document.Id, document.NodeId);
        return response.Resource;
    }

    public async Task<(IReadOnlyList<NodeDocument> Items, string? ContinuationToken)> QueryReadingsAsync(ReadingQueryParams parameters, CancellationToken cancellationToken = default)
    {
        var queryParts = new List<string> { "SELECT * FROM c WHERE c.type = 'SensorReading'" };

        if (!string.IsNullOrEmpty(parameters.NodeId))
            queryParts.Add(" AND c.nodeId = @nodeId");

        if (!string.IsNullOrEmpty(parameters.Sensor))
        {
            var sensorLower = parameters.Sensor.ToLowerInvariant();
            queryParts.Add(sensorLower switch
            {
                "noise" => " AND IS_DEFINED(c.noise)",
                "co2" => " AND IS_DEFINED(c.co2)",
                "temperature" => " AND IS_DEFINED(c.temperature)",
                "humidity" => " AND IS_DEFINED(c.humidity)",
                _ => ""
            });
        }

        if (parameters.Lat.HasValue && parameters.Lon.HasValue && parameters.LatDelta.HasValue && parameters.LonDelta.HasValue)
        {
            queryParts.Add(" AND c.location.lat >= @latMin AND c.location.lat <= @latMax AND c.location.lon >= @lonMin AND c.location.lon <= @lonMax");
        }
        else if (parameters.Lat.HasValue && parameters.Lon.HasValue)
        {
            queryParts.Add(" AND c.location.lat = @lat AND c.location.lon = @lon");
        }

        if (parameters.From.HasValue)
            queryParts.Add(" AND c.timestamp >= @from");

        if (parameters.To.HasValue)
            queryParts.Add(" AND c.timestamp <= @to");

        var queryText = string.Join("", queryParts) + " ORDER BY c.timestamp DESC";
        var queryDefinition = new QueryDefinition(queryText);

        if (!string.IsNullOrEmpty(parameters.NodeId))
            queryDefinition = queryDefinition.WithParameter("@nodeId", parameters.NodeId);
        if (parameters.Lat.HasValue && parameters.Lon.HasValue && parameters.LatDelta.HasValue && parameters.LonDelta.HasValue)
        {
            queryDefinition = queryDefinition
                .WithParameter("@latMin", parameters.Lat!.Value - parameters.LatDelta!.Value)
                .WithParameter("@latMax", parameters.Lat.Value + parameters.LatDelta.Value)
                .WithParameter("@lonMin", parameters.Lon!.Value - parameters.LonDelta!.Value)
                .WithParameter("@lonMax", parameters.Lon.Value + parameters.LonDelta.Value);
        }
        else if (parameters.Lat.HasValue && parameters.Lon.HasValue)
        {
            queryDefinition = queryDefinition
                .WithParameter("@lat", parameters.Lat!.Value)
                .WithParameter("@lon", parameters.Lon!.Value);
        }
        if (parameters.From.HasValue)
            queryDefinition = queryDefinition.WithParameter("@from", parameters.From.Value.ToString("O"));
        if (parameters.To.HasValue)
            queryDefinition = queryDefinition.WithParameter("@to", parameters.To.Value.ToString("O"));

        var maxCount = parameters.MaxCount ?? 100;
        var options = new QueryRequestOptions
        {
            MaxItemCount = maxCount
        };

        if (!string.IsNullOrEmpty(parameters.NodeId))
            options.PartitionKey = new PartitionKey(parameters.NodeId);

        using var iterator = _container.GetItemQueryIterator<NodeDocument>(queryDefinition, requestOptions: options, continuationToken: parameters.ContinuationToken);

        var results = new List<NodeDocument>();
        string? continuationToken = null;

        while (iterator.HasMoreResults && results.Count < maxCount)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            continuationToken = response.ContinuationToken;
            foreach (var item in response)
            {
                results.Add(item);
                if (results.Count >= maxCount) break;
            }
        }

        return (results, continuationToken);
    }

    public async Task<IReadOnlyList<NodeDocument>> GetLatestReadingsAsync(string? nodeId, double? lat, double? lon, double? latDelta, double? lonDelta, int maxCount = 50, CancellationToken cancellationToken = default)
    {
        var parameters = new ReadingQueryParams
        {
            NodeId = nodeId,
            Lat = lat,
            Lon = lon,
            LatDelta = latDelta,
            LonDelta = lonDelta,
            MaxCount = maxCount
        };

        var (items, _) = await QueryReadingsAsync(parameters, cancellationToken);
        return items;
    }
}
