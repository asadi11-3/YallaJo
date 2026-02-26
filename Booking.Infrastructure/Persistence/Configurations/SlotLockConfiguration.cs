using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class SlotLockConfiguration : IEntityTypeConfiguration<SlotLock>
{
    public void Configure(EntityTypeBuilder<SlotLock> builder)
    {
        builder.ToTable("SlotLocks", "booking");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.AvailabilitySlotId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.LockedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.IsReleased).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.ReleasedAt).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.AvailabilitySlot)
            .WithMany()
            .HasForeignKey(x => x.AvailabilitySlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.AvailabilitySlotId, x.IsReleased });
    }
}
