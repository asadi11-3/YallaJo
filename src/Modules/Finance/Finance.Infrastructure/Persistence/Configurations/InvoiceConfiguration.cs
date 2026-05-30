using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices", "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.PaymentId).IsRequired();
        builder.Property(x => x.BookingId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ProviderId).IsRequired();

        builder.Property(x => x.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(32)
            .IsUnicode(false);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<byte>()
            .HasDefaultValue(Finance.Domain.Enums.InvoiceStatus.Issued);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .IsUnicode(false);

        builder.OwnsOne(x => x.AmountSubtotal, money =>
        {
            money.Property(m => m.Amount).HasColumnName("AmountSubtotal").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("AmountSubtotalCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.OwnsOne(x => x.AmountTax, money =>
        {
            money.Property(m => m.Amount).HasColumnName("AmountTax").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("AmountTaxCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.OwnsOne(x => x.AmountDiscount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("AmountDiscount").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("AmountDiscountCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.OwnsOne(x => x.AmountTotal, money =>
        {
            money.Property(m => m.Amount).HasColumnName("AmountTotal").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("AmountTotalCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });

        builder.Property(x => x.IssuedAt).IsRequired();

        builder.Property(x => x.BuyerName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.BuyerEmail).IsRequired().HasMaxLength(320);
        builder.Property(x => x.SellerName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.SellerTaxId).IsRequired(false).HasMaxLength(50);

        builder.Property(x => x.PdfStoragePath).IsRequired(false).HasMaxLength(500);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.OwnsMany(x => x.Items, items =>
        {
            items.ToTable("InvoiceItems", "finance");
            items.WithOwner().HasForeignKey(i => i.InvoiceId);
            items.HasKey(i => i.Id);
            items.Property(i => i.Id).ValueGeneratedNever();
            items.Property(i => i.Description).IsRequired().HasMaxLength(500);
            items.Property(i => i.Quantity).IsRequired();
            items.OwnsOne(i => i.UnitPrice, money =>
            {
                money.Property(m => m.Amount).HasColumnName("UnitPrice").HasPrecision(19, 4);
                money.Property(m => m.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(3).HasDefaultValue("JOD");
            });
            items.OwnsOne(i => i.Subtotal, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Subtotal").HasPrecision(19, 4);
                money.Property(m => m.Currency).HasColumnName("SubtotalCurrency").HasMaxLength(3).HasDefaultValue("JOD");
            });

            items.Property(i => i.CreatedAt).IsRequired();
            items.Property(i => i.UpdatedAt).IsRequired(false);
        });

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.IssuedAt });
        builder.HasIndex(x => new { x.ProviderId, x.IssuedAt });
        builder.HasIndex(x => x.PaymentId).IsUnique();
        builder.HasIndex(x => x.BookingId);
    }
}
