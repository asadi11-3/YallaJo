using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

internal sealed class PaymentExpectationConfiguration : IEntityTypeConfiguration<PaymentExpectation>
{
    public void Configure(EntityTypeBuilder<PaymentExpectation> builder)
    {
        builder.ToTable("PaymentExpectations", "finance");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BookingId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ProviderId).IsRequired();
        builder.Property(x => x.TourId).IsRequired();

        builder.OwnsOne(x => x.ExpectedAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("ExpectedAmount")
                .HasColumnType("decimal(19,4)")
                .IsRequired();
            money.Property(m => m.Currency)
                .HasColumnName("ExpectedAmountCurrency")
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("JOD")
                .IsRequired();
        });

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsUnicode(false)
            .HasDefaultValue("JOD")
            .IsRequired();

        builder.Property(x => x.Reference)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<byte>()
            .HasDefaultValue(PaymentExpectationStatus.AwaitingPayment)
            .IsRequired();

        builder.Property(x => x.PaymentId);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.HasIndex(x => x.BookingId).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ProviderId);
    }
}
