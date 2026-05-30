using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.ToTable("Payouts", "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // ── New grouping key ──
        builder.Property(x => x.ProviderId).IsRequired();
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3).IsUnicode(false);

        // ── Period ──
        builder.Property(x => x.BatchPeriodStart).IsRequired();
        builder.Property(x => x.BatchPeriodEnd).IsRequired();

        // ── Status / lifecycle ──
        builder.Property(x => x.Status).IsRequired().HasConversion<byte>().HasDefaultValue(PayoutStatus.Pending);
        builder.Property(x => x.ProcessedAt).IsRequired(false);
        builder.Property(x => x.FailureReason).IsRequired(false).HasMaxLength(500);

        // ── Approval ──
        builder.Property(x => x.ApprovedByUserId).IsRequired(false);
        builder.Property(x => x.ApprovedAt).IsRequired(false);

        // ── Gateway ──
        builder.Property(x => x.GatewayPayoutId).IsRequired(false).HasMaxLength(200).IsUnicode(false);
        builder.Property(x => x.CompletedAt).IsRequired(false);

        // ── Recipient bank ──
        builder.Property(x => x.BankAccountId).IsRequired(false);

        // ── New Money totals ──
        builder.OwnsOne(x => x.GrossAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("GrossAmount").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("GrossAmountCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.OwnsOne(x => x.CommissionAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("CommissionAmount").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("CommissionAmountCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.OwnsOne(x => x.NetAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("NetAmount").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("NetAmountCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });

        // ── Legacy compatibility (preserved until migration) ──
        builder.Property(x => x.RecipientUserId).IsRequired();
        builder.OwnsOne(x => x.TotalAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("TotalAmount").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("TotalAmountCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.Property(x => x.BankAccountInfo).IsRequired(false).HasMaxLength(500);
        builder.Property(x => x.TransactionId).IsRequired(false).HasMaxLength(200).IsUnicode(false);
        builder.Property(x => x.Notes).IsRequired(false).HasMaxLength(1000);

        // ── Audit ──
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        // ── Relationships ──
        builder.HasMany(x => x.PayoutItems)
            .WithOne(x => x.Payout)
            .HasForeignKey(x => x.PayoutId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Indexes ──
        builder.HasIndex(x => new { x.ProviderId, x.Status });
        builder.HasIndex(x => x.Status).HasFilter("[Status] = 1"); // ReadyForPayout
        builder.HasIndex(x => new { x.BatchPeriodStart, x.BatchPeriodEnd });

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
