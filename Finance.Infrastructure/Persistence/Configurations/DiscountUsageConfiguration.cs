using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public class DiscountUsageConfiguration : IEntityTypeConfiguration<DiscountUsage>
{
    public void Configure(EntityTypeBuilder<DiscountUsage> builder)
    {
        builder.ToTable("DiscountUsages", "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.DiscountId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.BookingId).IsRequired(false);
        builder.Property(x => x.Amount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.UsedAt).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.Discount)
            .WithMany(x => x.DiscountUsages)
            .HasForeignKey(x => x.DiscountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
