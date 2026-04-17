using IoTNetwork.Core.Abstractions.Repositories;
using IoTNetwork.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTNetwork.Infrastructure.Persistence.Repositories;

public sealed class TelemetryReadingRepository(IoTNetworkDbContext dbContext) : ITelemetryReadingRepository
{
    public async Task AddAsync(TelemetryReading entity, CancellationToken cancellationToken = default)
    {
        await dbContext.TelemetryReadings.AddAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TelemetryReading>> GetByNodeAndRangeAsync(
        string nodeId,
        DateTime fromUtc,
        DateTime toUtc,
        int maxItems,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(maxItems, 1, 500);
        return await dbContext.TelemetryReadings
            .AsNoTracking()
            .Where(r => r.NodeId == nodeId && r.TimestampUtc >= fromUtc && r.TimestampUtc <= toUtc)
            .OrderByDescending(r => r.TimestampUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
