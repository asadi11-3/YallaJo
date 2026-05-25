using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public sealed class GuideTourOfferingConfiguration : IEntityTypeConfiguration<GuideTourOffering>
{
    public void Configure(EntityTypeBuilder<GuideTourOffering> builder)
    {
        builder.ToTable("GuideTourOfferings", "content_tours");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TourId).IsRequired();
        builder.Property(x => x.TourGuideId).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.OffersPrivateTour).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.PrivateTourPriceMultiplier).IsRequired(false).HasPrecision(5, 2);
        builder.Property(x => x.PrivateTourFlatPrice).IsRequired(false).HasPrecision(19, 4);
        builder.Property(x => x.IsProposer).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.ApplicationId).IsRequired(false);
        builder.Property(x => x.AssignedByUserId).IsRequired(false);
        builder.Property(x => x.SuspensionReason).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.SuspendedByAdminId).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.TourId, x.TourGuideId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.TourGuideId);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
