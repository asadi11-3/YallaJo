using ContentSeo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentSeo.Infrastructure.Persistence.Configurations;

public class SitemapEntryConfiguration : IEntityTypeConfiguration<SitemapEntry>
{
    public void Configure(EntityTypeBuilder<SitemapEntry> builder)
    {
        builder.ToTable("SitemapEntries", "content_seo");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Url)
            .IsRequired()
            .HasMaxLength(2048)
            .IsUnicode(false);

        builder.Property(x => x.ChangeFrequency)
            .IsRequired(false)
            .HasMaxLength(20)
            .IsUnicode(false);

        builder.Property(x => x.Priority)
            .IsRequired(false)
            .HasPrecision(2, 1);

        builder.Property(x => x.LastModified)
            .IsRequired(false);

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.EntityId)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.Url).IsUnique();
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}
