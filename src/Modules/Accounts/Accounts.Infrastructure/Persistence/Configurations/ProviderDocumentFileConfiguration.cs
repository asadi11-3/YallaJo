using Accounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounts.Infrastructure.Persistence.Configurations;

public sealed class ProviderDocumentFileConfiguration : IEntityTypeConfiguration<ProviderDocumentFile>
{
    public void Configure(EntityTypeBuilder<ProviderDocumentFile> builder)
    {
        builder.ToTable("ProviderDocumentFiles", "accounts");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.ProviderDocumentId).IsRequired();

        // Plain Guid reference into content_core.FileAssets. NO cross-module database
        // foreign key by design (module boundary): referential integrity is enforced
        // in the application layer, not by the database.
        builder.Property(f => f.FileAssetId).IsRequired();

        builder.Property(f => f.DocumentType)
            .IsRequired()
            .HasMaxLength(100)
            .HasConversion<string>();

        builder.Property(f => f.CreatedAt).IsRequired();
        builder.Property(f => f.UpdatedAt).IsRequired(false);

        // In-module foreign key to the owning provider document. Cascade is safe here:
        // it stays inside the accounts schema and never touches content_core.FileAssets.
        builder.HasOne<ProviderDocument>()
            .WithMany()
            .HasForeignKey(f => f.ProviderDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // One provider document maps to exactly one current file asset.
        builder.HasIndex(f => f.ProviderDocumentId)
            .IsUnique()
            .HasDatabaseName("UX_ProviderDocumentFiles_ProviderDocumentId");

        // Reverse lookup by file asset (non-unique: the same physical asset could,
        // in principle, be referenced again in the future).
        builder.HasIndex(f => f.FileAssetId)
            .HasDatabaseName("IX_ProviderDocumentFiles_FileAssetId");
    }
}
