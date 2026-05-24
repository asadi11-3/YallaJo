using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class CommissionSnapshotConfiguration : IEntityTypeConfiguration<CommissionSnapshot>
{
    public void Configure(EntityTypeBuilder<CommissionSnapshot> builder)
    {
        builder.ToTable("CommissionSnapshots", "booking");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Tier).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3).IsUnicode(false);
        builder.Property(x => x.Percentage).IsRequired().HasPrecision(5, 2);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.LastUpdatedAt).IsRequired();

        // BaseEntity inherited fields (no soft-delete/RowVersion — pure projection).
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasIndex(x => new { x.IsActive, x.Tier, x.Currency })
            .HasDatabaseName("IX_CommissionSnapshots_IsActive_Tier_Currency");
    }
}
