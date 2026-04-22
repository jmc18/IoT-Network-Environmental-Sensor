using System.Reflection;
using IoTNetwork.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTNetwork.Infrastructure.Persistence;

public sealed class IoTNetworkDbContext(DbContextOptions<IoTNetworkDbContext> options) : DbContext(options)
{
    public DbSet<TelemetryReading> TelemetryReadings => Set<TelemetryReading>();

    public DbSet<NodeDataDay> NodeDataDays => Set<NodeDataDay>();

    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
