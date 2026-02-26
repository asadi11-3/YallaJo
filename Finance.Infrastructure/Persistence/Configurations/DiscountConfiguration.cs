using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
    public void Configure(EntityTypeBuilder<Discount> builder)
    {
        builder.ToTable("Discounts", "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Code).IsRequired().HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.DiscountType).IsRequired().HasConversion<int>();
        builder.OwnsOne(x => x.DiscountValue, money =>
        {
            money.Property(m => m.Amount).HasColumnName("DiscountValue").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("DiscountValueCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.OwnsOne(x => x.MinOrderAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("MinOrderAmount").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("MinOrderAmountCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.OwnsOne(x => x.MaxDiscountAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("MaxDiscountAmount").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("MaxDiscountAmountCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.Property(x => x.MaxUsageCount).IsRequired(false);
        builder.Property(x => x.CurrentUsageCount).IsRequired().HasDefaultValue(0);
        builder.OwnsOne(x => x.ValidityPeriod, dr =>
        {
            dr.Property(d => d.Start).HasColumnName("ValidFrom");
            dr.Property(d => d.End).HasColumnName("ValidTo");
        });
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.EntityType).IsRequired(false).HasMaxLength(200);
        builder.Property(x => x.EntityId).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.DiscountUsages)
            .WithOne(x => x.Discount)
            .HasForeignKey(x => x.DiscountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
