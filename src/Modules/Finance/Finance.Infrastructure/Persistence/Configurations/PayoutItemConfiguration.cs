using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public class PayoutItemConfiguration : IEntityTypeConfiguration<PayoutItem>
{
    public void Configure(EntityTypeBuilder<PayoutItem> builder)
    {
        builder.ToTable("PayoutItems", "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.PayoutId).IsRequired();
        builder.Property(x => x.BookingId).IsRequired();
        builder.Property(x => x.CommissionRuleSnapshotId).IsRequired(false);

        builder.OwnsOne(x => x.GrossAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("AmountCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.OwnsOne(x => x.CommissionAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Commission").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("CommissionCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.OwnsOne(x => x.NetAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("NetAmount").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("NetAmountCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.Payout)
            .WithMany(x => x.PayoutItems)
            .HasForeignKey(x => x.PayoutId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PayoutId);
        builder.HasIndex(x => x.BookingId);

        builder.HasQueryFilter(x => !x.Payout.IsDeleted);
    }
}
