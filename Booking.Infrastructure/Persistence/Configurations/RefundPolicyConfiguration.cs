using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class RefundPolicyConfiguration : IEntityTypeConfiguration<RefundPolicy>
{
    public void Configure(EntityTypeBuilder<RefundPolicy> builder)
    {
        builder.ToTable("RefundPolicies", "booking");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.FullRefundHours).IsRequired();
        builder.Property(x => x.PartialRefundHours).IsRequired();
        builder.Property(x => x.PartialRefundPercent).IsRequired().HasPrecision(5, 2);
        builder.Property(x => x.IsDefault).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.IsDefault);
        builder.HasIndex(x => x.IsActive);
    }
}
