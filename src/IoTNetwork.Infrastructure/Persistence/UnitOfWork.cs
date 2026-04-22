using IoTNetwork.Core.Abstractions.Persistence;
using IoTNetwork.Core.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IoTNetwork.Infrastructure.Persistence;

public sealed class UnitOfWork(
    IoTNetworkDbContext dbContext,
    ITelemetryReadingRepository telemetryReadings,
    INodeDataDayRepository nodeDataDays,
    IDeviceTokenRepository deviceTokens) : IUnitOfWork
{
    public ITelemetryReadingRepository TelemetryReadings => telemetryReadings;

    public INodeDataDayRepository NodeDataDays => nodeDataDays;

    public IDeviceTokenRepository DeviceTokens => deviceTokens;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public void Dispose()
    {
        // DbContext lifetime is managed by the DI scope.
    }
}
