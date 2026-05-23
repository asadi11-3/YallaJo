using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
{
    public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
    {
        builder.ToTable("AvailabilitySlots", "booking", t => t.HasCheckConstraint(
            "CK_AvailSlots_Capacity",
            "[BookedCount] + [LockedCount] <= [MaxCapacity]"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TourGuideId).IsRequired();
        builder.Property(x => x.SlotType).IsRequired().HasConversion<int>();
        builder.Property(x => x.TourId).IsRequired(false);
        builder.Property(x => x.BusinessId).IsRequired(false);
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();
        builder.Property(x => x.MaxCapacity).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.BookedCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.LockedCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.PriceOverride).HasPrecision(19, 4);
        builder.Property(x => x.PriceOverrideCurrency).HasMaxLength(3);
        builder.Property(x => x.ScheduleId).IsRequired(false);
        builder.Property(x => x.ServiceItemId).IsRequired(false);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.TourGuide)
            .WithMany(x => x.AvailabilitySlots)
            .HasForeignKey(x => x.TourGuideId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TourGuideId, x.Date, x.StartTime, x.EndTime });
        builder.HasIndex(x => x.IsActive);

        builder.HasIndex(x => new { x.TourId, x.Date })
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("IX_AvailabilitySlots_TourId_Date_Active");
    }
}
