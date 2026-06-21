using ContentCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentCore.Infrastructure.Persistence.Configurations;

public class PromoBlockConfiguration : IEntityTypeConfiguration<PromoBlock>
{
    public void Configure(EntityTypeBuilder<PromoBlock> builder)
    {
        builder.ToTable("PromoBlocks", "content_core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.PlacementKey)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(100);

        builder.Property(x => x.Title)
            .IsRequired()
            .IsUnicode(true)
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired(false)
            .IsUnicode(true)
            .HasMaxLength(1000);

        builder.Property(x => x.ImageUrl)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(2048);

        builder.Property(x => x.AttachmentId)
            .IsRequired(false);

        builder.Property(x => x.ButtonText)
            .IsRequired(false)
            .IsUnicode(true)
            .HasMaxLength(100);

        builder.Property(x => x.ButtonUrl)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(2048);

        builder.Property(x => x.BadgeText)
            .IsRequired(false)
            .IsUnicode(true)
            .HasMaxLength(60);

        builder.Property(x => x.IconName)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(60);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.StartsAt).IsRequired(false);
        builder.Property(x => x.EndsAt).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.PlacementKey).IsUnique();
    }
}
