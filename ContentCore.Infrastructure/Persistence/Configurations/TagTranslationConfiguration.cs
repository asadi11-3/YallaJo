using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentCore.Infrastructure.Persistence.Configurations;

public class TagTranslationConfiguration : IEntityTypeConfiguration<TagTranslation>
{
    public void Configure(EntityTypeBuilder<TagTranslation> builder)
    {
        builder.ToTable("TagTranslations", "content_core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TagId).IsRequired();
        builder.Property(x => x.LanguageId).IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .IsUnicode(true)
            .HasMaxLength(100);

        builder.Property(x => x.Slug)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasDefaultValue(TranslationStatus.AutoTranslated)
            .HasSentinel(TranslationStatus.AutoTranslated)
            .HasConversion<byte>();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.Tag)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.Tag.IsDeleted);

        // One translation per language per tag
        builder.HasIndex(x => new { x.TagId, x.LanguageId })
            .IsUnique()
            .HasDatabaseName("UX_TagTranslations_TagId_LanguageId");

        builder.HasIndex(x => x.LanguageId)
            .HasDatabaseName("IX_TagTranslations_LanguageId");
    }
}
