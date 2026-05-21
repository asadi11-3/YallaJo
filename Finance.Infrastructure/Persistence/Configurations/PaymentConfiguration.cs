using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.BookingId).IsRequired(false);
        builder.Property(x => x.ReservationId).IsRequired(false);

        builder.OwnsOne(x => x.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("AmountCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3).IsUnicode(false);
        builder.Property(x => x.PaymentMethod).IsRequired().HasConversion<int>();
        builder.Property(x => x.Status).IsRequired().HasConversion<int>().HasDefaultValue(Finance.Domain.Enums.PaymentStatus.Pending);
        builder.Property(x => x.TransactionId).IsRequired(false).HasMaxLength(200).IsUnicode(false);
        builder.Property(x => x.GatewayResponse).IsRequired(false).HasColumnType("nvarchar(max)");
        builder.Property(x => x.PaidAt).IsRequired(false);
        builder.OwnsOne(x => x.RefundedAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("RefundedAmount").HasPrecision(19, 4).HasDefaultValue(0m);
            money.Property(m => m.Currency).HasColumnName("RefundedAmountCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.OwnsOne(x => x.RefundedTotal, money =>
        {
            money.Property(m => m.Amount).HasColumnName("RefundedTotal").HasPrecision(19, 4).HasDefaultValue(0m);
            money.Property(m => m.Currency).HasColumnName("RefundedTotalCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.Property(x => x.RefundedAt).IsRequired(false);

        // ── T1/T2 new columns ──
        builder.Property(x => x.ProviderId).IsRequired();
        builder.Property(x => x.PaymentType).IsRequired().HasConversion<byte>().HasDefaultValue(Finance.Domain.Enums.PaymentType.Booking);
        builder.Property(x => x.OriginalPaymentId).IsRequired(false);
        builder.Property(x => x.GatewayProvider).IsRequired().HasMaxLength(50).IsUnicode(false).HasDefaultValue(string.Empty);
        builder.Property(x => x.GatewayTransactionId).IsRequired(false).HasMaxLength(200).IsUnicode(false);
        builder.Property(x => x.RedirectUrl).IsRequired(false).HasMaxLength(1000)
            .HasConversion(v => v == null ? null : v.ToString(), s => string.IsNullOrEmpty(s) ? null : new Uri(s));
        builder.Property(x => x.ClientSecret).IsRequired(false).HasMaxLength(500).IsUnicode(false);
        builder.Property(x => x.RecipientAccount).IsRequired().HasMaxLength(100).HasDefaultValue("platform-escrow");
        builder.Property(x => x.ExpiresAt).IsRequired(false);
        builder.Property(x => x.EscrowReleaseEligibleAt).IsRequired(false);
        builder.Property(x => x.RetryCount).IsRequired().HasDefaultValue(0);

        // ── Audit ──
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.Disputes)
            .WithOne(x => x.Payment)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
        // F-R3 idempotency: unique filtered index on GatewayTransactionId (where NOT NULL)
        builder.HasIndex(x => x.GatewayTransactionId)
            .HasFilter("[GatewayTransactionId] IS NOT NULL")
            .IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => new { x.BookingId, x.Status });
        builder.HasIndex(x => new { x.ProviderId, x.Status });
        builder.HasIndex(x => x.EscrowReleaseEligibleAt);
        builder.HasIndex(x => new { x.PaymentType, x.Status, x.UpdatedAt });
    }
}
