using Backend.Configuration;
using Backend.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Backend.Data;

public sealed class CosmosTelemetryStore : ICosmosTelemetryStore
{
    private const int DefaultMaxItemsCap = 500;
    private const string PartitionPath = "/nodeId";

    private readonly CosmosClient _client;
    private readonly CosmosOptions _options;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private Database? _database;
    private Container? _telemetryContainer;
    private Container? _indexContainer;
    private bool _initialized;

    public CosmosTelemetryStore(CosmosClient client, IOptions<CosmosOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<TelemetryDocument> IngestAsync(string nodeId, TelemetryIngestRequest request, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        var timestamp = (request.TimestampUtc ?? DateTime.UtcNow).ToUniversalTime();
        var id = Guid.NewGuid().ToString("N");
        var telemetry = new TelemetryDocument
        {
            Id = id,
            NodeId = nodeId,
            TimestampUtc = timestamp,
            Temperature = request.Temperature,
            Humidity = request.Humidity,
            Co2 = request.Co2,
            NoiseLevel = request.NoiseLevel,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
        };

        var dayKey = timestamp.ToString("yyyy-MM-dd");
        var indexDoc = new NodeDayIndexDocument
        {
            Id = dayKey,
            NodeId = nodeId,
            DayUtc = dayKey,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        var pk = new PartitionKey(nodeId);
        await _telemetryContainer!
            .CreateItemAsync(telemetry, pk, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await _indexContainer!
            .UpsertItemAsync(indexDoc, pk, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return telemetry;
    }

    public async Task<PagedTelemetryResponse> GetReadingsAsync(
        string nodeId,
        DateTime fromUtc,
        DateTime toUtc,
        int maxItems,
        string? continuationToken,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        var take = Math.Clamp(maxItems, 1, DefaultMaxItemsCap);
        var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.nodeId = @nodeId AND c.timestampUtc >= @from AND c.timestampUtc <= @to ORDER BY c.timestampUtc DESC")
            .WithParameter("@nodeId", nodeId)
            .WithParameter("@from", fromUtc)
            .WithParameter("@to", toUtc);

        var iterator = _telemetryContainer!.GetItemQueryIterator<TelemetryDocument>(
            query,
            continuationToken,
            new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(nodeId),
                MaxItemCount = take,
            });

        if (!iterator.HasMoreResults)
        {
            return new PagedTelemetryResponse { Items = Array.Empty<TelemetryReadingView>(), ContinuationToken = null };
        }

        var page = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
        var views = page.Select(TelemetryReadingView.FromDocument).ToList();
        return new PagedTelemetryResponse
        {
            Items = views,
            ContinuationToken = page.ContinuationToken,
        };
    }

    public async Task<IReadOnlyList<string>> GetAvailableDatesAsync(string nodeId, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        var query = new QueryDefinition("SELECT VALUE c.id FROM c WHERE c.nodeId = @nodeId ORDER BY c.id ASC")
            .WithParameter("@nodeId", nodeId);

        var iterator = _indexContainer!.GetItemQueryIterator<string>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(nodeId) });

        var dates = new List<string>();
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
            dates.AddRange(page);
        }

        return dates;
    }

    public async Task<IReadOnlyList<string>> ListNodesAsync(CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        var query = new QueryDefinition("SELECT DISTINCT VALUE c.nodeId FROM c");

        var iterator = _indexContainer!.GetItemQueryIterator<string>(query);

        var nodes = new HashSet<string>(StringComparer.Ordinal);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
            foreach (var n in page)
            {
                if (!string.IsNullOrWhiteSpace(n))
                {
                    nodes.Add(n);
                }
            }
        }

        return nodes.OrderBy(n => n, StringComparer.Ordinal).ToList();
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            _database = await _client.CreateDatabaseIfNotExistsAsync(_options.DatabaseName, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var telemetryProps = new ContainerProperties(_options.TelemetryContainerName, PartitionPath);
            var indexProps = new ContainerProperties(_options.NodeDataIndexContainerName, PartitionPath);

            _telemetryContainer = await _database.CreateContainerIfNotExistsAsync(
                    telemetryProps,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _indexContainer = await _database.CreateContainerIfNotExistsAsync(
                    indexProps,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
