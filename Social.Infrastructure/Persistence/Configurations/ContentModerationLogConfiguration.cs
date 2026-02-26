using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Social.Domain.Entities;

namespace Social.Infrastructure.Persistence.Configurations;

public class ContentModerationLogConfiguration : IEntityTypeConfiguration<ContentModerationLog>
{
    public void Configure(EntityTypeBuilder<ContentModerationLog> builder)
    {
        builder.ToTable("ContentModerationLogs", "social");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(200);
        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.ModeratorUserId).IsRequired();
        builder.Property(x => x.Action).IsRequired().HasConversion<int>();
        builder.Property(x => x.Reason).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.OccurredAt).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}
