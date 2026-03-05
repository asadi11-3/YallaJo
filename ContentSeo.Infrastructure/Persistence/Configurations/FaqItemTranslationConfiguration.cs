using ContentSeo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentSeo.Infrastructure.Persistence.Configurations;

public class FaqItemTranslationConfiguration : IEntityTypeConfiguration<FaqItemTranslation>
{
    public void Configure(EntityTypeBuilder<FaqItemTranslation> builder)
    {
        builder.ToTable("FaqItemTranslations", "content_seo");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.FaqItemId).IsRequired();
        builder.Property(x => x.LanguageId).IsRequired();

        builder.Property(x => x.Question)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.Answer)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.FaqItem)
            .WithMany(x => x.FaqItemTranslations)
            .HasForeignKey(x => x.FaqItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.FaqItem.IsDeleted);
    }
}
