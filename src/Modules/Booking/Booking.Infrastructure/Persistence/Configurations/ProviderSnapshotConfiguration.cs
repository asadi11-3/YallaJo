using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

internal sealed class ProviderSnapshotConfiguration : IEntityTypeConfiguration<ProviderSnapshot>
{
    public void Configure(EntityTypeBuilder<ProviderSnapshot> builder)
    {
        builder.ToTable("ProviderSnapshots", "booking");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProviderId).IsRequired();
        builder.Property(x => x.OwnerUserId).IsRequired();
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.LastUpdatedAt).IsRequired();

        builder.HasIndex(x => x.ProviderId).IsUnique().HasDatabaseName("IX_ProviderSnapshots_ProviderId");
        builder.HasIndex(x => x.OwnerUserId).HasDatabaseName("IX_ProviderSnapshots_OwnerUserId");
    }
}
