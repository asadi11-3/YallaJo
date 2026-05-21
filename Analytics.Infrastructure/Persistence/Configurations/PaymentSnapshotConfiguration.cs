using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class PaymentSnapshotConfiguration : IEntityTypeConfiguration<PaymentSnapshot>
{
    public void Configure(EntityTypeBuilder<PaymentSnapshot> builder)
    {
        builder.ToTable("PaymentSnapshots", "analytics");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.CommissionAmount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.Status).HasMaxLength(50);
        builder.Property(x => x.Type).HasMaxLength(50);
        builder.HasIndex(x => x.PaymentId).IsUnique();
        builder.HasIndex(x => x.ProviderId);
    }
}
