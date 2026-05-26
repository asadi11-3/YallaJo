using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class GuideDiscountConfiguration : IEntityTypeConfiguration<GuideDiscount>
{
    public void Configure(EntityTypeBuilder<GuideDiscount> builder)
    {
        builder.ToTable("GuideDiscounts", "booking");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.GuideUserId).IsRequired();
        builder.Property(x => x.TourId).IsRequired(false);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.DiscountType).IsRequired().HasConversion<int>();
        builder.Property(x => x.DiscountValue).IsRequired().HasPrecision(9, 4);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3).IsUnicode(false);
        builder.Property(x => x.ValidFrom).IsRequired();
        builder.Property(x => x.ValidUntil).IsRequired(false);
        builder.Property(x => x.MaxUsageCount).IsRequired(false);
        builder.Property(x => x.CurrentUsageCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.GuideUserId);
        builder.HasIndex(x => new { x.GuideUserId, x.TourId });
        builder.HasIndex(x => new { x.IsActive, x.ValidFrom, x.ValidUntil });
    }
}
