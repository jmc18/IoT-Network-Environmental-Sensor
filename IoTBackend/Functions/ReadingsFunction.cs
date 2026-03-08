using System.Diagnostics;
using Functions.Services;
using IoTBackend.Shared.Logging;
using Microsoft.Extensions.Logging;
using IoTBackend.Shared.Models;
using IoTBackend.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Functions;

public class ReadingsFunction
{
    private readonly ICosmosDbService _cosmosDb;
    private readonly ILogger<ReadingsFunction> _logger;

    public ReadingsFunction(ICosmosDbService cosmosDb, ILogger<ReadingsFunction> logger)
    {
        _cosmosDb = cosmosDb;
        _logger = logger;
    }

    [Function("GetReadings")]
    public async Task<IActionResult> GetReadings(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "api/readings")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var nodeId = req.Query["nodeId"].FirstOrDefault();
        var sensor = req.Query["sensor"].FirstOrDefault();
        var latStr = req.Query["lat"].FirstOrDefault();
        var lonStr = req.Query["lon"].FirstOrDefault();
        var latDeltaStr = req.Query["latDelta"].FirstOrDefault();
        var lonDeltaStr = req.Query["lonDelta"].FirstOrDefault();
        var fromStr = req.Query["from"].FirstOrDefault();
        var toStr = req.Query["to"].FirstOrDefault();
        var maxCountStr = req.Query["maxCount"].FirstOrDefault();
        var continuationToken = req.Query["continuationToken"].FirstOrDefault();

        double? lat = ParseDouble(latStr);
        double? lon = ParseDouble(lonStr);
        double? latDelta = ParseDouble(latDeltaStr);
        double? lonDelta = ParseDouble(lonDeltaStr);
        DateTimeOffset? from = ParseDateTime(fromStr);
        DateTimeOffset? to = ParseDateTime(toStr);
        var maxCount = int.TryParse(maxCountStr, out var mc) ? mc : 100;

        var parameters = new ReadingQueryParams
        {
            NodeId = string.IsNullOrEmpty(nodeId) ? null : nodeId,
            Sensor = string.IsNullOrEmpty(sensor) ? null : sensor,
            Lat = lat,
            Lon = lon,
            LatDelta = latDelta,
            LonDelta = lonDelta,
            From = from,
            To = to,
            MaxCount = Math.Min(maxCount, 500),
            ContinuationToken = string.IsNullOrEmpty(continuationToken) ? null : continuationToken
        };

        var (items, nextToken) = await _cosmosDb.QueryReadingsAsync(parameters, cancellationToken);

        sw.Stop();
        _logger.LogQueryRequest(nodeId, sensor, lat, lon, items.Count, sw.ElapsedMilliseconds);

        var result = new ReadingQueryResult
        {
            Readings = items,
            ContinuationToken = nextToken
        };

        return new OkObjectResult(result);
    }

    [Function("GetLatestReadings")]
    public async Task<IActionResult> GetLatestReadings(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "api/readings/latest")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var nodeId = req.Query["nodeId"].FirstOrDefault();
        var latStr = req.Query["lat"].FirstOrDefault();
        var lonStr = req.Query["lon"].FirstOrDefault();
        var latDeltaStr = req.Query["latDelta"].FirstOrDefault();
        var lonDeltaStr = req.Query["lonDelta"].FirstOrDefault();
        var maxCountStr = req.Query["maxCount"].FirstOrDefault();

        double? lat = ParseDouble(latStr);
        double? lon = ParseDouble(lonStr);
        double? latDelta = ParseDouble(latDeltaStr);
        double? lonDelta = ParseDouble(lonDeltaStr);
        var maxCount = int.TryParse(maxCountStr, out var mc) ? Math.Min(mc, 100) : 50;

        var items = await _cosmosDb.GetLatestReadingsAsync(
            string.IsNullOrEmpty(nodeId) ? null : nodeId,
            lat, lon, latDelta, lonDelta,
            maxCount,
            cancellationToken);

        return new OkObjectResult(new ReadingQueryResult { Readings = items });
    }

    private static double? ParseDouble(string? s)
        => double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;

    private static DateTimeOffset? ParseDateTime(string? s)
        => DateTimeOffset.TryParse(s, out var v) ? v : null;
}
