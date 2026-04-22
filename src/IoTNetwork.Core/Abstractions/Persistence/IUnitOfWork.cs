using IoTNetwork.Core.Abstractions.Repositories;

namespace IoTNetwork.Core.Abstractions.Persistence;

public interface IUnitOfWork : IDisposable
{
    ITelemetryReadingRepository TelemetryReadings { get; }

    INodeDataDayRepository NodeDataDays { get; }

    IDeviceTokenRepository DeviceTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
