using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class UserPreferredCategoryConfiguration : IEntityTypeConfiguration<UserPreferredCategory>
{
    public void Configure(EntityTypeBuilder<UserPreferredCategory> builder)
    {
        builder.ToTable("UserPreferredCategories", "analytics");

        builder.HasKey(x => new { x.UserId, x.CategoryId });

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CategoryId).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.Property(x => x.PreferenceScore)
            .IsRequired()
            .HasPrecision(5, 4)
            .HasDefaultValue(0m);
    }
}
