using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("InvoiceItems", "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.Status).IsRequired().HasConversion<int>().HasDefaultValue(InvoiceStatus.Draft);
        builder.Property(x => x.SubTotal).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.TaxAmount).IsRequired().HasPrecision(19, 4).HasDefaultValue(0m);
        builder.Property(x => x.TotalAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3).IsUnicode(false);
        builder.Property(x => x.DueDate).IsRequired();
        builder.Property(x => x.PaidAt).IsRequired(false);
        builder.Property(x => x.Notes).IsRequired(false).HasColumnType("nvarchar(max)");

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.InvoiceLineItems)
            .WithOne(x => x.InvoiceItem)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
    }
}
