using ContentPlaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentPlaces.Infrastructure.Persistence.Configurations;

public class BusinessTranslationConfiguration : IEntityTypeConfiguration<BusinessTranslation>
{
    public void Configure(EntityTypeBuilder<BusinessTranslation> builder)
    {
        builder.ToTable("BusinessTranslations", "content_places");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.BusinessId).IsRequired();
        builder.Property(x => x.LanguageId).IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(300);

        builder.Property(x => x.Description)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Address)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.Business)
            .WithMany(x => x.BusinessTranslations)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.BusinessId, x.LanguageId }).IsUnique();
        builder.HasIndex(x => x.LanguageId);
    }
}
