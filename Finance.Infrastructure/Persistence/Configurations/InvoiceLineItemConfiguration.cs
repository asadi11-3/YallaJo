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
        builder.Property(x => x.UnitPrice).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.Amount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.EntityType).IsRequired(false).HasMaxLength(200);
        builder.Property(x => x.EntityId).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.InvoiceItem)
            .WithMany(x => x.InvoiceLineItems)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
