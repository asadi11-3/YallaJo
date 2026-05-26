using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentCore.Infrastructure.Persistence.Configurations;

public class TranslationCacheConfiguration : IEntityTypeConfiguration<TranslationCache>
{
    public void Configure(EntityTypeBuilder<TranslationCache> builder)
    {
        builder.ToTable("TranslationCaches", "content_core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.OriginalText)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.OriginalTextHash)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(64);

        builder.Property(x => x.TranslatedText)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.FromLanguage)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(10);

        builder.Property(x => x.ToLanguage)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(10);

        builder.Property(x => x.Confidence).IsRequired(false);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasDefaultValue(TranslationStatus.AutoTranslated)
            .HasSentinel(TranslationStatus.AutoTranslated)
            .HasConversion<byte>();

        builder.Property(x => x.EntityType)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(50);

        builder.Property(x => x.EntityId).IsRequired(false);

        builder.Property(x => x.FieldName)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        // POSSIBLE-001: Concurrency token so optimistic-locking catch blocks are functional.
        builder.Property(x => x.RowVersion).IsRowVersion();

        // Unique dedup index by deterministic hash of (OriginalText + FromLanguage + ToLanguage)
        builder.HasIndex(x => x.OriginalTextHash)
            .IsUnique()
            .HasDatabaseName("UX_TranslationCache_Hash");

        // Secondary lookup index for analytics/listing by language pair
        builder.HasIndex(x => new { x.FromLanguage, x.ToLanguage })
            .HasDatabaseName("IX_TranslationCache_Lookup");

        // Index for entity-based lookups
        builder.HasIndex(x => new { x.EntityType, x.EntityId })
            .HasDatabaseName("IX_TranslationCache_Entity")
            .HasFilter("[EntityType] IS NOT NULL");
    }
}
