using IoTNetwork.Api.Realtime;
using IoTNetwork.Api.Validation;
using IoTNetwork.Core.Abstractions.Notifications;
using IoTNetwork.Core.Abstractions.Persistence;
using IoTNetwork.Core.Application.Dtos;
using IoTNetwork.Core.Domain.Entities;
using Mapster;
using Microsoft.AspNetCore.SignalR;

namespace IoTNetwork.Api.Endpoints;

public static class TelemetryRoutes
{
    private const double MaxRangeDays = 90;

    public static void MapTelemetryRoutes(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/nodes", async (IUnitOfWork uow, CancellationToken ct) =>
        {
            var nodes = await uow.NodeDataDays.GetDistinctNodeIdsAsync(ct).ConfigureAwait(false);
            return Results.Ok(new NodesListDto { Nodes = nodes });
        });

        api.MapGet("/nodes/{nodeId}/available-dates", async (string nodeId, IUnitOfWork uow, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                return Results.BadRequest("nodeId is required.");
            }

            var days = await uow.NodeDataDays.GetDaysForNodeAsync(nodeId.Trim(), ct).ConfigureAwait(false);
            var dates = days.Select(d => d.ToString("yyyy-MM-dd")).ToList();
            return Results.Ok(new AvailableDatesDto { Dates = dates });
        });

        api.MapGet("/nodes/{nodeId}/readings", async (
            string nodeId,
            DateTime from,
            DateTime to,
            int? maxItems,
            IUnitOfWork uow,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                return Results.BadRequest("nodeId is required.");
            }

            var fromUtc = NormalizeUtc(from);
            var toUtc = NormalizeUtc(to);
            if (toUtc < fromUtc)
            {
                return Results.BadRequest("'to' must be greater than or equal to 'from'.");
            }

            if ((toUtc - fromUtc).TotalDays > MaxRangeDays)
            {
                return Results.BadRequest($"Date range must not exceed {MaxRangeDays} days.");
            }

            var take = maxItems ?? 50;
            var items = await uow.TelemetryReadings.GetByNodeAndRangeAsync(nodeId.Trim(), fromUtc, toUtc, take, ct)
                .ConfigureAwait(false);
            var dtos = items.Select(r => r.Adapt<TelemetryReadingDto>()).ToList();
            return Results.Ok(new PagedReadingsDto { Items = dtos });
        });

        api.MapPost("/ingest/nodes/{nodeId}/readings", async (
            string nodeId,
            TelemetryIngestDto body,
            IUnitOfWork uow,
            IHubContext<TelemetryHub> hub,
            ICriticalTelemetryNotifier notifier,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                return Results.BadRequest("nodeId is required.");
            }

            if (!TelemetryIngestValidator.TryValidate(body, out var err))
            {
                return Results.BadRequest(err);
            }

            var entity = body.Adapt<TelemetryReading>();
            entity.Id = Guid.NewGuid();
            entity.NodeId = nodeId.Trim();
            entity.TimestampUtc = (body.TimestampUtc ?? DateTime.UtcNow).ToUniversalTime();

            var day = DateOnly.FromDateTime(entity.TimestampUtc);
            var index = new NodeDataDay
            {
                NodeId = entity.NodeId,
                DayUtc = day,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            await uow.TelemetryReadings.AddAsync(entity, ct).ConfigureAwait(false);
            await uow.NodeDataDays.UpsertDayAsync(index, ct).ConfigureAwait(false);
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);

            var dto = entity.Adapt<TelemetryReadingDto>();

            await hub.Clients.Group(TelemetryHub.GroupName(entity.NodeId))
                .SendAsync("reading", dto, ct).ConfigureAwait(false);
            await hub.Clients.All.SendAsync("readingAny", dto, ct).ConfigureAwait(false);

            _ = notifier.NotifyIfCriticalAsync(entity, CancellationToken.None);

            return Results.Created($"/api/nodes/{Uri.EscapeDataString(entity.NodeId)}/readings", dto);
        });

        api.MapPost("/push/register", async (
            DeviceTokenRegisterDto body,
            IUnitOfWork uow,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Token))
            {
                return Results.BadRequest("Token is required.");
            }

            var token = new DeviceToken
            {
                Id = Guid.NewGuid(),
                Token = body.Token.Trim(),
                NodeFilter = string.IsNullOrWhiteSpace(body.NodeFilter) ? null : body.NodeFilter!.Trim(),
                CreatedAtUtc = DateTime.UtcNow,
                LastSeenUtc = DateTime.UtcNow,
            };

            await uow.DeviceTokens.UpsertAsync(token, ct).ConfigureAwait(false);
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);
            return Results.Ok(new { ok = true });
        });

        api.MapPost("/push/unregister", async (
            DeviceTokenUnregisterDto body,
            IUnitOfWork uow,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Token))
            {
                return Results.BadRequest("Token is required.");
            }

            await uow.DeviceTokens.RemoveByTokenAsync(body.Token.Trim(), ct).ConfigureAwait(false);
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);
            return Results.Ok(new { ok = true });
        });

        api.MapGet("/health", () => Results.Ok(new { status = "ok" }));
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
}
