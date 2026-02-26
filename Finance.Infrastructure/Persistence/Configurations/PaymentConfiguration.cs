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

        builder.Property(x => x.Amount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3).IsUnicode(false);
        builder.Property(x => x.PaymentMethod).IsRequired().HasConversion<int>();
        builder.Property(x => x.Status).IsRequired().HasConversion<int>().HasDefaultValue(Finance.Domain.Enums.PaymentStatus.Pending);
        builder.Property(x => x.TransactionId).IsRequired(false).HasMaxLength(200).IsUnicode(false);
        builder.Property(x => x.GatewayResponse).IsRequired(false).HasColumnType("nvarchar(max)");
        builder.Property(x => x.PaidAt).IsRequired(false);
        builder.Property(x => x.RefundedAmount).IsRequired().HasPrecision(19, 4).HasDefaultValue(0m);
        builder.Property(x => x.RefundedAt).IsRequired(false);

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
        builder.HasIndex(x => new { x.UserId, x.Status });
    }
}
