using IoTNetwork.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IoTNetwork.Infrastructure.Persistence.Configurations;

public sealed class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.ToTable("device_tokens");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Token)
            .HasMaxLength(512)
            .IsRequired();

        builder.HasIndex(e => e.Token)
            .IsUnique()
            .HasDatabaseName("ix_device_tokens_token");

        builder.Property(e => e.NodeFilter)
            .HasMaxLength(128);

        builder.Property(e => e.CreatedAtUtc).IsRequired();
        builder.Property(e => e.LastSeenUtc).IsRequired();
    }
}
