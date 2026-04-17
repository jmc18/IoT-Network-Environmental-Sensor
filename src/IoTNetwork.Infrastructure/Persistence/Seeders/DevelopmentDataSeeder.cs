using IoTNetwork.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IoTNetwork.Infrastructure.Persistence.Seeders;

/// <summary>
/// Inserts deterministic sample telemetry for local development only (invoked from the host when configured).
/// </summary>
public sealed class DevelopmentDataSeeder(IoTNetworkDbContext dbContext, ILogger<DevelopmentDataSeeder> logger)
{
    public const string NodeIdPrefix = "local-seed-";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var hasSeedData = await dbContext.TelemetryReadings
            .AnyAsync(r => r.NodeId.StartsWith(NodeIdPrefix), cancellationToken)
            .ConfigureAwait(false);

        if (hasSeedData)
        {
            logger.LogInformation("Development seed skipped: rows with prefix {Prefix} already exist.", NodeIdPrefix);
            return;
        }

        var now = DateTime.UtcNow;
        var readings = new List<TelemetryReading>();
        var days = new HashSet<(string NodeId, DateOnly Day)>();

        readings.AddRange(CreateSeriesForNode(
            nodeId: $"{NodeIdPrefix}mexico-city",
            baseLat: 19.432608,
            baseLng: -99.133209,
            nowUtc: now,
            readingsPerDay: 4,
            daySpan: 5,
            rngSeed: 101));

        readings.AddRange(CreateSeriesForNode(
            nodeId: $"{NodeIdPrefix}guadalajara",
            baseLat: 20.659698,
            baseLng: -103.349609,
            nowUtc: now,
            readingsPerDay: 3,
            daySpan: 4,
            rngSeed: 202));

        foreach (var r in readings)
        {
            days.Add((r.NodeId, DateOnly.FromDateTime(r.TimestampUtc)));
        }

        await dbContext.TelemetryReadings.AddRangeAsync(readings, cancellationToken).ConfigureAwait(false);

        var dayEntities = days.Select(d => new NodeDataDay
        {
            NodeId = d.NodeId,
            DayUtc = d.Day,
            UpdatedAtUtc = now,
        }).ToList();

        await dbContext.NodeDataDays.AddRangeAsync(dayEntities, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Development seed completed: {Readings} readings across {Nodes} nodes and {Days} day-index rows.",
            readings.Count,
            readings.Select(r => r.NodeId).Distinct().Count(),
            dayEntities.Count);
    }

    private static IEnumerable<TelemetryReading> CreateSeriesForNode(
        string nodeId,
        double baseLat,
        double baseLng,
        DateTime nowUtc,
        int readingsPerDay,
        int daySpan,
        int rngSeed)
    {
        var rng = new Random(rngSeed);
        for (var d = 0; d < daySpan; d++)
        {
            var day = DateOnly.FromDateTime(nowUtc).AddDays(-d);
            var dayStartUtc = DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            for (var i = 0; i < readingsPerDay; i++)
            {
                var hour = 6 + (i * 4) % 14;
                var minute = (i * 17) % 60;
                var timestamp = dayStartUtc.AddHours(hour).AddMinutes(minute);

                yield return new TelemetryReading
                {
                    Id = Guid.NewGuid(),
                    NodeId = nodeId,
                    TimestampUtc = timestamp,
                    Temperature = 18 + rng.NextDouble() * 12 + d * 0.15,
                    Humidity = 40 + rng.NextDouble() * 35,
                    Co2 = 420 + rng.NextDouble() * 180 + d * 5,
                    NoiseLevel = 32 + rng.NextDouble() * 18,
                    Latitude = baseLat + (rng.NextDouble() - 0.5) * 0.02,
                    Longitude = baseLng + (rng.NextDouble() - 0.5) * 0.02,
                };
            }
        }
    }
}
