using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public class CommissionRuleConfiguration : IEntityTypeConfiguration<CommissionRule>
{
    public void Configure(EntityTypeBuilder<CommissionRule> builder)
    {
        builder.ToTable("CommissionRules", "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.CommissionPercentage).IsRequired().HasPrecision(5, 2);
        builder.OwnsOne(x => x.MinAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("MinAmount").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("MinAmountCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.OwnsOne(x => x.MaxAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("MaxAmount").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("MaxAmountCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(200);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.Priority).IsRequired().HasDefaultValue(0);
        builder.OwnsOne(x => x.ValidityPeriod, dr =>
        {
            dr.Property(d => d.Start).HasColumnName("ValidFrom");
            dr.Property(d => d.End).HasColumnName("ValidTo");
        });

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
