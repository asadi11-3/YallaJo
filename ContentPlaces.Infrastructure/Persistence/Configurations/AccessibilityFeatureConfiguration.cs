using ContentPlaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentPlaces.Infrastructure.Persistence.Configurations;

public class AccessibilityFeatureConfiguration : IEntityTypeConfiguration<AccessibilityFeature>
{
    public void Configure(EntityTypeBuilder<AccessibilityFeature> builder)
    {
        builder.ToTable("AccessibilityFeatures", "content_places");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.EntityType).IsRequired();
        builder.Property(x => x.EntityId).IsRequired();

        builder.Property(x => x.FeatureType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Name)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(1000);

        builder.Property(x => x.IsAvailable)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.FeatureType);
    }
}
