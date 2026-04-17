using IoTNetwork.Core.Domain.Entities;

namespace IoTNetwork.Core.Abstractions.Repositories;

public interface ITelemetryReadingRepository
{
    Task AddAsync(TelemetryReading entity, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelemetryReading>> GetByNodeAndRangeAsync(
        string nodeId,
        DateTime fromUtc,
        DateTime toUtc,
        int maxItems,
        CancellationToken cancellationToken = default);
}
