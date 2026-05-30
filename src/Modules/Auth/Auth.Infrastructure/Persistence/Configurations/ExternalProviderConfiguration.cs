using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

public sealed class ExternalProviderConfiguration : IEntityTypeConfiguration<ExternalProvider>
{
    public void Configure(EntityTypeBuilder<ExternalProvider> builder)
    {
        builder.ToTable("ExternalProviders", "auth");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.UserId).IsRequired();

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ProviderUserId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.ProviderEmail)
            .HasMaxLength(320)
            .IsRequired(false);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.VerifiedAt).IsRequired(false);

        // Auditable fields
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired(false);
        builder.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.DeletedAt).IsRequired(false);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => e.UserId);

        // Uniqueness is enforced ONLY for active links.
        // Without this filter, deactivating (unlinking) a provider account would permanently
        // block re-linking it — the DB constraint would fire even though IsActive = false.
        builder.HasIndex(e => new { e.Provider, e.ProviderUserId })
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_ExternalProviders_Provider_ProviderUserId_Active");

        builder.HasIndex(e => e.IsActive);
    }
}
