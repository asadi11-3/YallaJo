using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.ToTable("UserPreferences", "analytics");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.BudgetTier).HasMaxLength(50);
        builder.Property(x => x.IsFamilyTraveler).HasDefaultValue(false);
        builder.Property(x => x.CurrentTripStage).HasMaxLength(50);
        builder.Property(x => x.LastComputedAt);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
