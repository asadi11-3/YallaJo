using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Social.Domain.Entities;

namespace Social.Infrastructure.Persistence.Configurations;

public sealed class BookingEligibilitySnapshotConfiguration : IEntityTypeConfiguration<BookingEligibilitySnapshot>
{
    public void Configure(EntityTypeBuilder<BookingEligibilitySnapshot> builder)
    {
        builder.ToTable("BookingEligibilitySnapshots", "social");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.TargetType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.TargetId).IsRequired();
        builder.Property(x => x.FirstCompletedAt).IsRequired();
        builder.Property(x => x.LastCompletedAt).IsRequired();
        builder.Property(x => x.CompletedBookingCount).IsRequired().HasDefaultValue(0);

        // BaseEntity only (no AuditableEntity — no soft-delete on snapshots)
        builder.Property(x => x.CreatedAt).IsRequired();

        // One snapshot per (UserId, TargetType, TargetId)
        builder.HasIndex(x => new { x.UserId, x.TargetType, x.TargetId })
            .IsUnique()
            .HasDatabaseName("IX_BookingEligibilitySnapshots_User_Target_Unique");

        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_BookingEligibilitySnapshots_UserId");
    }
}
