using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Backend.Functions;

public sealed class NodesListFunction
{
    private readonly ICosmosTelemetryStore _store;
    private readonly ILogger<NodesListFunction> _logger;

    public NodesListFunction(ICosmosTelemetryStore store, ILogger<NodesListFunction> logger)
    {
        _store = store;
        _logger = logger;
    }

    [Function(nameof(ListNodes))]
    public async Task<IActionResult> ListNodes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "nodes")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            var nodes = await _store.ListNodesAsync(cancellationToken).ConfigureAwait(false);
            return new OkObjectResult(new NodesListResponse { Nodes = nodes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list nodes.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
