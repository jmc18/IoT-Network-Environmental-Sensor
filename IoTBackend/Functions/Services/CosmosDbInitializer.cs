using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Functions.Services;

/// <summary>
/// Ensures Cosmos DB database and container exist at startup (for local dev with emulator).
/// </summary>
public class CosmosDbInitializer : IHostedService
{
    private const string DatabaseId = "iot-sensors";
    private const string ContainerId = "readings";
    private const string PartitionKeyPath = "/nodeId";

    private readonly CosmosClient _client;
    private readonly ILogger<CosmosDbInitializer> _logger;

    public CosmosDbInitializer(CosmosClient client, ILogger<CosmosDbInitializer> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var database = await _client.CreateDatabaseIfNotExistsAsync(DatabaseId, cancellationToken: cancellationToken);
            await database.Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(ContainerId, PartitionKeyPath) { DefaultTimeToLive = -1 },
                throughput: 400,
                cancellationToken: cancellationToken);
            _logger.LogInformation("Cosmos DB initialized: database={Database}, container={Container}", DatabaseId, ContainerId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cosmos DB initialization failed (container may already exist)");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
