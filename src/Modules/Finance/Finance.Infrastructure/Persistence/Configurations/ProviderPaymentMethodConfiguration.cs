using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public sealed class ProviderPaymentMethodConfiguration : IEntityTypeConfiguration<ProviderPaymentMethod>
{
    public void Configure(EntityTypeBuilder<ProviderPaymentMethod> builder)
    {
        builder.ToTable("ProviderPaymentMethods", "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.PaymentMethodType).IsRequired().HasConversion<int>();
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.IsVerified).IsRequired();
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(120);
        builder.Property(x => x.AccountIdentifier).IsRequired().HasMaxLength(120);
        builder.Property(x => x.BankName).IsRequired(false).HasMaxLength(200);
        builder.Property(x => x.VerifiedAt).IsRequired(false);
        builder.Property(x => x.VerifiedByAdminId).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.AccountIdentifier }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.IsDefault });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
