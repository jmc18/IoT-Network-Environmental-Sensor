using IoTNetwork.Core.Domain.Entities;

namespace IoTNetwork.Core.Abstractions.Repositories;

public interface IDeviceTokenRepository
{
    Task UpsertAsync(DeviceToken token, CancellationToken cancellationToken = default);

    Task RemoveByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceToken>> GetForNodeAsync(string nodeId, CancellationToken cancellationToken = default);
}
