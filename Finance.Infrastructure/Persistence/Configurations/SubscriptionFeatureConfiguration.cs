using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public class SubscriptionFeatureConfiguration : IEntityTypeConfiguration<SubscriptionFeature>
{
    public void Configure(EntityTypeBuilder<SubscriptionFeature> builder)
    {
        builder.ToTable("SubscriptionFeatures", "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired(false).HasMaxLength(500);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(100).IsUnicode(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
