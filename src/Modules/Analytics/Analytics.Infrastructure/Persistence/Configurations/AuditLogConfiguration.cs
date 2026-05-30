using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", "analytics");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasConversion<byte>().IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(256);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.IpAddressHash).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.OldValue).HasColumnType("nvarchar(max)");
        builder.Property(x => x.NewValue).HasColumnType("nvarchar(max)");
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => x.RedactedAt).HasFilter("[RedactedAt] IS NOT NULL");
    }
}
