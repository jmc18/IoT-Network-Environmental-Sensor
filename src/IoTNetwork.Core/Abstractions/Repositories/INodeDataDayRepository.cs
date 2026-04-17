using IoTNetwork.Core.Domain.Entities;

namespace IoTNetwork.Core.Abstractions.Repositories;

public interface INodeDataDayRepository
{
    Task UpsertDayAsync(NodeDataDay entity, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DateOnly>> GetDaysForNodeAsync(string nodeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDistinctNodeIdsAsync(CancellationToken cancellationToken = default);
}
