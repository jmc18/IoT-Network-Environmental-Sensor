using Backend.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Backend.Functions;

public sealed class ReadingsFunction
{
    private readonly ICosmosTelemetryStore _store;
    private readonly ILogger<ReadingsFunction> _logger;

    public ReadingsFunction(ICosmosTelemetryStore store, ILogger<ReadingsFunction> logger)
    {
        _store = store;
        _logger = logger;
    }

    [Function(nameof(GetReadings))]
    public async Task<IActionResult> GetReadings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "nodes/{nodeId}/readings")]
        HttpRequest req,
        string nodeId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return new BadRequestObjectResult(new { error = "nodeId is required." });
        }

        if (!TryParseUtcQuery(req, "from", out var fromUtc, out var fromError))
        {
            return new BadRequestObjectResult(new { error = fromError });
        }

        if (!TryParseUtcQuery(req, "to", out var toUtc, out var toError))
        {
            return new BadRequestObjectResult(new { error = toError });
        }

        if (toUtc < fromUtc)
        {
            return new BadRequestObjectResult(new { error = "'to' must be greater than or equal to 'from'." });
        }

        const double maxRangeDays = 90;
        if ((toUtc - fromUtc).TotalDays > maxRangeDays)
        {
            return new BadRequestObjectResult(new { error = $"Date range must not exceed {maxRangeDays} days." });
        }

        var maxItems = 50;
        if (int.TryParse(req.Query["maxItems"].FirstOrDefault(), out var parsed))
        {
            maxItems = parsed;
        }

        var continuation = req.Query["continuationToken"].FirstOrDefault();

        try
        {
            var page = await _store
                .GetReadingsAsync(nodeId.Trim(), fromUtc, toUtc, maxItems, continuation, cancellationToken)
                .ConfigureAwait(false);
            return new OkObjectResult(page);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query readings for node {NodeId}.", nodeId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    private static bool TryParseUtcQuery(HttpRequest req, string name, out DateTime utc, out string? error)
    {
        error = null;
        utc = default;
        var raw = req.Query[name].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = $"Query parameter '{name}' is required (UTC ISO-8601).";
            return false;
        }

        if (!DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
        {
            error = $"Query parameter '{name}' must be a valid UTC ISO-8601 timestamp.";
            return false;
        }

        utc = parsed.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc) : parsed.ToUniversalTime();
        return true;
    }
}
