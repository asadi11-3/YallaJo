using ContentPlaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentPlaces.Infrastructure.Persistence.Configurations;

public class PlaceTranslationConfiguration : IEntityTypeConfiguration<PlaceTranslation>
{
    public void Configure(EntityTypeBuilder<PlaceTranslation> builder)
    {
        builder.ToTable("PlaceTranslations", "content_places");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.PlaceId).IsRequired();
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

        builder.HasOne(x => x.Place)
            .WithMany(x => x.PlaceTranslations)
            .HasForeignKey(x => x.PlaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.PlaceId, x.LanguageId }).IsUnique();
        builder.HasIndex(x => x.LanguageId);
    }
}
