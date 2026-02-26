using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public class LoyaltyPointsConfiguration : IEntityTypeConfiguration<LoyaltyPoints>
{
    public void Configure(EntityTypeBuilder<LoyaltyPoints> builder)
    {
        builder.ToTable("LoyaltyPoints", "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.TotalPoints).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.AvailablePoints).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.LifetimePoints).IsRequired().HasDefaultValue(0);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.Transactions)
            .WithOne(x => x.LoyaltyPoints)
            .HasForeignKey(x => x.LoyaltyPointsId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
