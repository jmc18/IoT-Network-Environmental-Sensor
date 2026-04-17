using IoTNetwork.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IoTNetwork.Infrastructure.Persistence.Configurations;

public sealed class NodeDataDayConfiguration : IEntityTypeConfiguration<NodeDataDay>
{
    public void Configure(EntityTypeBuilder<NodeDataDay> builder)
    {
        builder.ToTable("node_data_days");

        builder.HasKey(e => new { e.NodeId, e.DayUtc });

        builder.Property(e => e.NodeId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.DayUtc)
            .IsRequired();

        builder.Property(e => e.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(e => e.NodeId)
            .HasDatabaseName("ix_node_data_days_node");
    }
}
