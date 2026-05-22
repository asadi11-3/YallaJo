using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class BoostPackageConfiguration : IEntityTypeConfiguration<BoostPackage>
{
    public void Configure(EntityTypeBuilder<BoostPackage> builder)
    {
        builder.ToTable("BoostPackages", "analytics");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityKind).HasConversion<byte>().IsRequired();
        builder.Property(x => x.BoostMultiplier).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.BillingMode).HasMaxLength(20).HasDefaultValue("FlatFee").IsRequired();
        builder.Property(x => x.BidPerClick).HasPrecision(18, 4);
        builder.Property(x => x.DailyBudgetCap).HasPrecision(18, 4);
        builder.Property(x => x.SpentToday).HasPrecision(18, 4).HasDefaultValue(0m);

        builder.HasIndex(x => new { x.EntityKind, x.EntityId, x.IsActive });
        builder.HasIndex(x => new { x.BillingMode, x.IsActive });
        builder.HasIndex(x => x.ProviderId);
        builder.HasIndex(x => x.ExpiresAt);
    }
}
