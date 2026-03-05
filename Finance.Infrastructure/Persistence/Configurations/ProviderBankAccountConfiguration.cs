using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public class ProviderBankAccountConfiguration : IEntityTypeConfiguration<ProviderBankAccount>
{
    public void Configure(EntityTypeBuilder<ProviderBankAccount> builder)
    {
        builder.ToTable("ProviderBankAccounts", "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.BankName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.AccountHolderName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.AccountNumber).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Iban).IsRequired(false).HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.SwiftCode).IsRequired(false).HasMaxLength(20).IsUnicode(false);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3).IsUnicode(false);
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.IsVerified).IsRequired();
        builder.Property(x => x.VerifiedAt).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.UserId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
