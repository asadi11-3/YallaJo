using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class TripArcConfiguration : IEntityTypeConfiguration<TripArc>
{
    public void Configure(EntityTypeBuilder<TripArc> builder)
    {
        builder.ToTable("TripArcs", "analytics");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.DayClustersJson).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.InterestTags).HasMaxLength(500);

        builder.HasIndex(x => x.IsActive);
    }
}
