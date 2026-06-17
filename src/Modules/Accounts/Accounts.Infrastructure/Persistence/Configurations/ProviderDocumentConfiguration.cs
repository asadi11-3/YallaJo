using Accounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounts.Infrastructure.Persistence.Configurations;

public sealed class ProviderDocumentConfiguration : IEntityTypeConfiguration<ProviderDocument>
{
    public void Configure(EntityTypeBuilder<ProviderDocument> builder)
    {
        builder.ToTable("ProviderDocuments", "accounts");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.ApplicationId).IsRequired();

        builder.Property(d => d.DocumentType)
            .IsRequired()
            .HasMaxLength(100)
            .HasConversion<string>();

        // Patch 2G: the physical file metadata (URL / name / size) moved to the FileAsset V2
        // model (content_core.FileAssets) linked via accounts.ProviderDocumentFiles. The legacy
        // FileUrl / FileName / FileSizeBytes columns are dropped from accounts.ProviderDocuments.
        builder.Property(d => d.ExpiresAt).IsRequired(false);

        builder.Property(d => d.UploadedAt).IsRequired();

        // Match the principal's soft-delete query filter so that documents of a
        // soft-deleted ProviderApplication are filtered out together with their parent.
        // Without this matching filter EF warns that the required principal can be
        // filtered out while the required dependent remains (linkid=2131316).
        builder.HasQueryFilter(d => !d.Application.IsDeleted);

        // Indexes
        builder.HasIndex(d => d.ApplicationId)
            .HasDatabaseName("IX_ProviderDocuments_ApplicationId");
    }
}
