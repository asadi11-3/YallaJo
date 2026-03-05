using ContentSeo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentSeo.Infrastructure.Persistence.Configurations;

public class SeoMetadataConfiguration : IEntityTypeConfiguration<SeoMetadata>
{
    public void Configure(EntityTypeBuilder<SeoMetadata> builder)
    {
        builder.ToTable("SeoMetadata", "content_seo");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.EntityId).IsRequired();

        builder.Property(x => x.MetaTitle)
            .IsRequired(false)
            .HasMaxLength(200);

        builder.Property(x => x.MetaDescription)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.CanonicalUrl)
            .IsRequired(false)
            .HasMaxLength(2048);

        builder.Property(x => x.OgTitle)
            .IsRequired(false)
            .HasMaxLength(200);

        builder.Property(x => x.OgDescription)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.OgImageUrl)
            .IsRequired(false)
            .HasMaxLength(2048);

        builder.Property(x => x.SchemaMarkup)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.SitemapPriority)
            .IsRequired()
            .HasPrecision(2, 1)
            .HasDefaultValue(0.5m);

        builder.Property(x => x.SitemapChangeFrequency)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(20);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => new { x.EntityType, x.EntityId }).IsUnique();
    }
}
