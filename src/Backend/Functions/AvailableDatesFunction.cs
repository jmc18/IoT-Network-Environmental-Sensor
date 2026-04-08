using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Backend.Functions;

public sealed class AvailableDatesFunction
{
    private readonly ICosmosTelemetryStore _store;
    private readonly ILogger<AvailableDatesFunction> _logger;

    public AvailableDatesFunction(ICosmosTelemetryStore store, ILogger<AvailableDatesFunction> logger)
    {
        _store = store;
        _logger = logger;
    }

    [Function(nameof(GetAvailableDates))]
    public async Task<IActionResult> GetAvailableDates(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "nodes/{nodeId}/available-dates")]
        HttpRequest req,
        string nodeId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return new BadRequestObjectResult(new { error = "nodeId is required." });
        }

        try
        {
            var dates = await _store.GetAvailableDatesAsync(nodeId.Trim(), cancellationToken).ConfigureAwait(false);
            return new OkObjectResult(new AvailableDatesResponse { Dates = dates });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read available dates for node {NodeId}.", nodeId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
