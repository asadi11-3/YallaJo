using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public sealed class TourProposalConfiguration : IEntityTypeConfiguration<TourProposal>
{
    public void Configure(EntityTypeBuilder<TourProposal> builder)
    {
        builder.ToTable("TourProposals", "content_tours");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TourGuideId).IsRequired();
        builder.Property(x => x.GuideUserId).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Description).IsRequired(false).HasMaxLength(4000);
        builder.Property(x => x.ShortDescription).IsRequired(false).HasMaxLength(512);
        builder.Property(x => x.PlaceId).IsRequired();
        builder.Property(x => x.DurationMinutes).IsRequired();
        builder.Property(x => x.MaxGroupSize).IsRequired();
        builder.Property(x => x.BasePrice).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.RequestExclusive).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.ReviewedByAdminId).IsRequired(false);
        builder.Property(x => x.ReviewedAt).IsRequired(false);
        builder.Property(x => x.RejectionReason).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.CreatedTourId).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.TourGuideId);
        builder.HasIndex(x => x.GuideUserId);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
