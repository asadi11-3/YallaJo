using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class RefundPolicyConfiguration : IEntityTypeConfiguration<RefundPolicy>
{
    public void Configure(EntityTypeBuilder<RefundPolicy> builder)
    {
        builder.ToTable("RefundPolicies", "booking");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TourId).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.OwnsMany(p => p.Tiers, tier =>
        {
            tier.ToJson();
            tier.Property(t => t.HoursBeforeTour).HasJsonPropertyName("hoursBeforeTour");
            tier.Property(t => t.RefundPercent)
                .HasJsonPropertyName("refundPercent")
                .HasPrecision(5, 2);
        });

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        // Filters + indexes
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.TourId).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}
