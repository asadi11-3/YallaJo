using Accounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounts.Infrastructure.Persistence.Configurations;

public sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("Profiles", "accounts");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        // Logical reference to Security.User — no FK
        builder.Property(p => p.UserId).IsRequired();

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.DisplayName)
            .IsRequired(false)
            .HasMaxLength(200);

        builder.Property(p => p.AvatarUrl)
            .IsRequired(false)
            .HasMaxLength(2048);

        builder.Property(p => p.DateOfBirth)
            .IsRequired(false);

        builder.Property(p => p.Gender)
            .IsRequired(false)
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(p => p.Country)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(p => p.City)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(p => p.AddressLine)
            .IsRequired(false)
            .HasMaxLength(300);

        builder.OwnsOne(p => p.MarketingConsent, consent =>
        {
            consent.Property(x => x.EmailDigest).HasColumnName("MarketingConsentEmailDigest");
            consent.Property(x => x.PushNotifications).HasColumnName("MarketingConsentPushNotifications");
            consent.Property(x => x.ReEngagementCampaigns).HasColumnName("MarketingConsentReEngagementCampaigns");
            consent.Property(x => x.LastUpdatedUtc).HasColumnName("MarketingConsentLastUpdatedUtc").IsRequired(false);
        });

        // Auditable fields
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired(false);
        builder.Property(p => p.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).IsRequired(false);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasQueryFilter(p => !p.IsDeleted);

        // Indexes — one profile per user
        builder.HasIndex(p => p.UserId)
            .IsUnique()
            .HasDatabaseName("IX_Profiles_UserId_Unique");
    }
}
