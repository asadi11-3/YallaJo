using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> builder)
    {
        builder.ToTable("Referrals", "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ReferrerUserId).IsRequired();
        builder.Property(x => x.ReferredUserId).IsRequired(false);
        builder.Property(x => x.ReferralCode).IsRequired().HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.Status).IsRequired().HasDefaultValue((byte)0);
        builder.Property(x => x.RewardAmount).IsRequired(false).HasPrecision(19, 4);
        builder.Property(x => x.ReferredRewardAmount).HasPrecision(19, 4);
        builder.Property(x => x.Currency).IsRequired(false).HasMaxLength(3).IsUnicode(false);
        builder.Property(x => x.CompletedAt).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.ReferralCode).IsUnique();
    }
}
