using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", "analytics");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.UserId).IsRequired(false);

        builder.Property(x => x.UserAgent)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.EntityId)
            .IsRequired(false)
            .HasMaxLength(450);

        builder.Property(x => x.IpAddress)
            .IsRequired(false)
            .HasMaxLength(45);

        builder.Property(x => x.OldValues)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.NewValues)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.OccurredAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}
