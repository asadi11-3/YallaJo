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

        builder.Property(d => d.FileUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(d => d.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.FileSizeBytes).IsRequired();

        builder.Property(d => d.ExpiresAt).IsRequired(false);

        builder.Property(d => d.UploadedAt).IsRequired();

        // Indexes
        builder.HasIndex(d => d.ApplicationId)
            .HasDatabaseName("IX_ProviderDocuments_ApplicationId");
    }
}
