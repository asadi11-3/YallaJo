using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class BookingSnapshotConfiguration : IEntityTypeConfiguration<BookingSnapshot>
{
    public void Configure(EntityTypeBuilder<BookingSnapshot> builder)
    {
        builder.ToTable("BookingSnapshots", "analytics");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.Status).HasMaxLength(50);
        builder.HasIndex(x => x.BookingId).IsUnique();
        builder.HasIndex(x => x.ProviderId);
    }
}
