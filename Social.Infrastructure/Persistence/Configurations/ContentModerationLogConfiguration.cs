using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Social.Domain.Entities;

namespace Social.Infrastructure.Persistence.Configurations;

public sealed class ContentModerationLogConfiguration : IEntityTypeConfiguration<ContentModerationLog>
{
    public void Configure(EntityTypeBuilder<ContentModerationLog> builder)
    {
        builder.ToTable("ContentModerationLogs", "social");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.AdminUserId).IsRequired();
        builder.Property(x => x.EntityType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.Action).IsRequired().HasConversion<byte>();
        builder.Property(x => x.Notes).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.ActionedAt).IsRequired();
        builder.Property(x => x.SourceReportId).IsRequired(false);

        // BaseEntity — CreatedAt only (append-only, no UpdatedAt/IsDeleted)
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.EntityType, x.EntityId })
            .HasDatabaseName("IX_ContentModerationLogs_EntityType_EntityId");

        builder.HasIndex(x => x.AdminUserId).HasDatabaseName("IX_ContentModerationLogs_AdminUserId");
        builder.HasIndex(x => x.ActionedAt).HasDatabaseName("IX_ContentModerationLogs_ActionedAt");
    }
}
