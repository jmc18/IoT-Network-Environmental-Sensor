using IoTNetwork.Core.Abstractions.Repositories;
using IoTNetwork.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTNetwork.Infrastructure.Persistence.Repositories;

public sealed class DeviceTokenRepository(IoTNetworkDbContext dbContext) : IDeviceTokenRepository
{
    public async Task UpsertAsync(DeviceToken token, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.DeviceTokens
            .FirstOrDefaultAsync(t => t.Token == token.Token, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            await dbContext.DeviceTokens.AddAsync(token, cancellationToken).ConfigureAwait(false);
            return;
        }

        existing.NodeFilter = token.NodeFilter;
        existing.LastSeenUtc = token.LastSeenUtc;
    }

    public async Task RemoveByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.DeviceTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            dbContext.DeviceTokens.Remove(existing);
        }
    }

    public async Task<IReadOnlyList<DeviceToken>> GetForNodeAsync(string nodeId, CancellationToken cancellationToken = default) =>
        await dbContext.DeviceTokens
            .AsNoTracking()
            .Where(t => t.NodeFilter == null || t.NodeFilter == nodeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
