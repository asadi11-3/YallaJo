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
        builder.Property(x => x.Amount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.Commission).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.NetAmount).IsRequired().HasPrecision(19, 4);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.Payout)
            .WithMany(x => x.PayoutItems)
            .HasForeignKey(x => x.PayoutId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
