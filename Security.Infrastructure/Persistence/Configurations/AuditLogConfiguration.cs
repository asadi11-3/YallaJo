using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Entities;

namespace Security.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", "security");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        // Nullable — system actions have no user. No FK (cross-module reference).
        builder.Property(a => a.UserId).IsRequired(false);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.ResourceType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.ResourceId).IsRequired(false);

        builder.Property(a => a.IpAddress)
            .IsRequired(false)
            .HasMaxLength(45); // IPv6 max

        builder.Property(a => a.OldValue)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.NewValue)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.OccurredAt).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        // Indexes
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.OccurredAt);
        builder.HasIndex(a => new { a.ResourceType, a.ResourceId });
    }
}
