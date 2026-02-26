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
        builder.Property(x => x.MinAmount).IsRequired(false).HasPrecision(19, 4);
        builder.Property(x => x.MaxAmount).IsRequired(false).HasPrecision(19, 4);
        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(200);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.Priority).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.ValidFrom).IsRequired(false);
        builder.Property(x => x.ValidTo).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
