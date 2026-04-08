using System.Text.Json;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Backend.Functions;

public sealed class TelemetryIngestFunction
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ICosmosTelemetryStore _store;
    private readonly ISignalRTelemetryPublisher _signalR;
    private readonly ILogger<TelemetryIngestFunction> _logger;

    public TelemetryIngestFunction(
        ICosmosTelemetryStore store,
        ISignalRTelemetryPublisher signalR,
        ILogger<TelemetryIngestFunction> logger)
    {
        _store = store;
        _signalR = signalR;
        _logger = logger;
    }

    [Function(nameof(IngestTelemetry))]
    public async Task<IActionResult> IngestTelemetry(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "nodes/{nodeId}/telemetry")]
        HttpRequest req,
        string nodeId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return new BadRequestObjectResult(new { error = "nodeId must be provided." });
        }

        TelemetryIngestRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<TelemetryIngestRequest>(req.Body, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in telemetry body.");
            return new BadRequestObjectResult(new { error = "Invalid JSON body." });
        }

        if (body is null)
        {
            return new BadRequestObjectResult(new { error = "Request body is required." });
        }

        if (!TelemetryIngestValidator.TryValidate(body, out var validationError))
        {
            return new BadRequestObjectResult(new { error = validationError });
        }

        TelemetryDocument stored;
        try
        {
            stored = await _store.IngestAsync(nodeId.Trim(), body, cancellationToken).ConfigureAwait(false);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Cosmos failure while ingesting telemetry for node {NodeId}.", nodeId);
            return new ObjectResult(new { error = "Storage operation failed." }) { StatusCode = StatusCodes.Status503ServiceUnavailable };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while ingesting telemetry for node {NodeId}.", nodeId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        var payload = new TelemetryUpdatedPayload
        {
            NodeId = stored.NodeId,
            ReadingId = stored.Id,
            TimestampUtc = stored.TimestampUtc,
            Temperature = stored.Temperature,
            Humidity = stored.Humidity,
            Co2 = stored.Co2,
            NoiseLevel = stored.NoiseLevel,
            Latitude = stored.Latitude,
            Longitude = stored.Longitude,
        };

        try
        {
            await _signalR.PublishReadingAsync(payload, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telemetry stored but SignalR broadcast failed for node {NodeId}.", nodeId);
        }

        return new CreatedResult($"/api/nodes/{Uri.EscapeDataString(stored.NodeId)}/readings", TelemetryReadingView.FromDocument(stored));
    }
}
