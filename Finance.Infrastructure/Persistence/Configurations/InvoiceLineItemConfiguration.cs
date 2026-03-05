using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("InvoiceLineItems", "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.InvoiceId).IsRequired();
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Quantity).IsRequired().HasDefaultValue(1);
        builder.OwnsOne(x => x.UnitPrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("UnitPrice").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.OwnsOne(x => x.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("AmountCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.Property(x => x.EntityType).IsRequired(false).HasMaxLength(200);
        builder.Property(x => x.EntityId).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.InvoiceItem)
            .WithMany(x => x.InvoiceLineItems)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.InvoiceItem.IsDeleted);
    }
}
