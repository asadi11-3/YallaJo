using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public class PlanFeatureConfiguration : IEntityTypeConfiguration<PlanFeature>
{
    public void Configure(EntityTypeBuilder<PlanFeature> builder)
    {
        builder.ToTable("PlanFeatures", "finance");

        builder.HasKey(x => new { x.PlanId, x.FeatureId });

        builder.Property(x => x.PlanId).IsRequired();
        builder.Property(x => x.FeatureId).IsRequired();
        builder.Property(x => x.Value).IsRequired(false).HasMaxLength(200);

        builder.HasOne(x => x.SubscriptionPlan)
            .WithMany(x => x.PlanFeatures)
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SubscriptionFeature)
            .WithMany()
            .HasForeignKey(x => x.FeatureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.SubscriptionFeature.IsDeleted);
    }
}
