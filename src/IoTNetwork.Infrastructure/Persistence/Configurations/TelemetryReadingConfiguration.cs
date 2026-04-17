using IoTNetwork.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IoTNetwork.Infrastructure.Persistence.Configurations;

public sealed class TelemetryReadingConfiguration : IEntityTypeConfiguration<TelemetryReading>
{
    public void Configure(EntityTypeBuilder<TelemetryReading> builder)
    {
        builder.ToTable("telemetry_readings");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NodeId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.TimestampUtc)
            .IsRequired();

        builder.HasIndex(e => new { e.NodeId, e.TimestampUtc })
            .HasDatabaseName("ix_telemetry_node_time");

        builder.Property(e => e.Temperature);
        builder.Property(e => e.Humidity);
        builder.Property(e => e.Co2);
        builder.Property(e => e.NoiseLevel);
        builder.Property(e => e.Latitude);
        builder.Property(e => e.Longitude);
    }
}
