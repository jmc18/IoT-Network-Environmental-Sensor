using Functions.Services;
using IoTBackend.Shared.Logging;
using IoTBackend.Shared.Services;
using IoTBackend.Shared.Models;
using IoTBackend.Shared.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Functions;

public class IngestFunction
{
    private readonly ICosmosDbService _cosmosDb;
    private readonly ISignalRService? _signalR;
    private readonly ILogger<IngestFunction> _logger;
    private readonly IConfiguration _configuration;

    public IngestFunction(
        ICosmosDbService cosmosDb,
        ILogger<IngestFunction> logger,
        IConfiguration configuration,
        ISignalRService? signalR = null)
    {
        _cosmosDb = cosmosDb;
        _logger = logger;
        _configuration = configuration;
        _signalR = signalR;
    }

    [Function("Ingest")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/ingest")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        if (!ApiKeyValidator.Validate(req, _configuration))
        {
            _logger.LogWarning("Ingest request rejected: invalid or missing API key");
            return new UnauthorizedObjectResult(new { error = "Invalid or missing API key" });
        }

        NodeReadingRequest? request;
        try
        {
            request = await req.ReadFromJsonAsync<NodeReadingRequest>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in ingest request");
            return new BadRequestObjectResult(new { error = "Invalid JSON payload" });
        }

        if (request is null || string.IsNullOrWhiteSpace(request.NodeId))
        {
            return new BadRequestObjectResult(new { error = "nodeId is required" });
        }

        _logger.LogIngestRequest(request.NodeId, request.Lat, request.Lon);

        var document = request.ToNodeDocument();
        var saved = await _cosmosDb.CreateReadingAsync(document, cancellationToken);

        if (_signalR is not null)
        {
            await _signalR.PublishNewReadingAsync(saved, cancellationToken);
            _logger.LogSignalRPublish(saved.NodeId);
        }

        return new OkObjectResult(new { id = saved.Id, nodeId = saved.NodeId, timestamp = saved.Timestamp });
    }
}
