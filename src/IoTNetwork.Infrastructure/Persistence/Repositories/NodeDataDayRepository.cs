using IoTNetwork.Core.Abstractions.Repositories;
using IoTNetwork.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTNetwork.Infrastructure.Persistence.Repositories;

public sealed class NodeDataDayRepository(IoTNetworkDbContext dbContext) : INodeDataDayRepository
{
    public async Task UpsertDayAsync(NodeDataDay entity, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.NodeDataDays
            .FindAsync(new object[] { entity.NodeId, entity.DayUtc }, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            await dbContext.NodeDataDays.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            existing.UpdatedAtUtc = entity.UpdatedAtUtc;
        }
    }

    public async Task<IReadOnlyList<DateOnly>> GetDaysForNodeAsync(string nodeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.NodeDataDays
            .AsNoTracking()
            .Where(d => d.NodeId == nodeId)
            .OrderBy(d => d.DayUtc)
            .Select(d => d.DayUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> GetDistinctNodeIdsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.NodeDataDays
            .AsNoTracking()
            .Select(d => d.NodeId)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
