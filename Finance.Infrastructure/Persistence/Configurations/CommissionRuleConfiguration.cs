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

        builder.Property(x => x.Tier).IsRequired().HasMaxLength(50);
        builder.Property(x => x.MinMonthlyRevenue).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.MaxMonthlyRevenue).IsRequired(false).HasPrecision(19, 4);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3).IsUnicode(false);
        builder.Property(x => x.Percentage).IsRequired().HasPrecision(5, 2);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.Notes).IsRequired(false).HasMaxLength(2000);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        // Indexes:
        //  (Tier, Currency) lookup index
        //  Unique filtered (Tier, Currency, MinMonthlyRevenue, MaxMonthlyRevenue) where IsDeleted=0
        builder.HasIndex(x => new { x.Tier, x.Currency });
        builder.HasIndex(x => new { x.Tier, x.Currency, x.MinMonthlyRevenue, x.MaxMonthlyRevenue })
            .HasFilter("[IsDeleted] = 0")
            .IsUnique();

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
