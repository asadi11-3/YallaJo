using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentCore.Infrastructure.Persistence.Configurations;

public class SpecializationTranslationConfiguration : IEntityTypeConfiguration<SpecializationTranslation>
{
    public void Configure(EntityTypeBuilder<SpecializationTranslation> builder)
    {
        builder.ToTable("SpecializationTranslations", "content_core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.SpecializationId).IsRequired();
        builder.Property(x => x.LanguageId).IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .IsUnicode(true)
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired(false)
            .IsUnicode(true)
            .HasMaxLength(1000);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasDefaultValue(TranslationStatus.AutoTranslated)
            .HasSentinel(TranslationStatus.AutoTranslated)
            .HasConversion<byte>();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.Specialization)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.SpecializationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.Specialization.IsDeleted);

        builder.HasIndex(x => new { x.SpecializationId, x.LanguageId })
            .IsUnique()
            .HasDatabaseName("UX_SpecializationTranslations_SpecId_LanguageId");

        builder.HasIndex(x => x.LanguageId)
            .HasDatabaseName("IX_SpecializationTranslations_LanguageId");
    }
}
