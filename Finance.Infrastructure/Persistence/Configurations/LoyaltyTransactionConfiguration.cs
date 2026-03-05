using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.ToTable("LoyaltyTransactions", "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.LoyaltyPointsId).IsRequired();
        builder.Property(x => x.Points).IsRequired();
        builder.Property(x => x.TransactionType).IsRequired().HasConversion<int>();
        builder.Property(x => x.Description).IsRequired(false).HasMaxLength(500);
        builder.Property(x => x.ReferenceId).IsRequired(false);
        builder.Property(x => x.ExpiresAt).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.LoyaltyPoints)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.LoyaltyPointsId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.LoyaltyPoints.IsDeleted);
    }
}
