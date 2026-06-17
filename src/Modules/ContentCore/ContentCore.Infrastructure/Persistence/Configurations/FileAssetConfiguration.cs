using ContentCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentCore.Infrastructure.Persistence.Configurations;

public sealed class FileAssetConfiguration : IEntityTypeConfiguration<FileAsset>
{
    public void Configure(EntityTypeBuilder<FileAsset> builder)
    {
        builder.ToTable("FileAssets", "content_core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.StorageProvider)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(50);

        builder.Property(x => x.StorageKey)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(1024);

        builder.Property(x => x.OriginalFileName)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(500);

        builder.Property(x => x.SafeFileName)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(500);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(100);

        builder.Property(x => x.Extension)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(32);

        builder.Property(x => x.SizeBytes).IsRequired();

        builder.Property(x => x.Sha256)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(64);

        builder.Property(x => x.UploadedByUserId).IsRequired();

        builder.Property(x => x.Width).IsRequired(false);
        builder.Property(x => x.Height).IsRequired(false);
        builder.Property(x => x.DurationSeconds).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.DeletedAt).IsRequired(false);

        // Soft-delete: only non-deleted assets are visible by default.
        builder.HasQueryFilter(x => !x.IsDeleted);

        // StorageKey is the physical locator and must be globally unique per provider/key.
        builder.HasIndex(x => x.StorageKey)
            .IsUnique()
            .HasDatabaseName("UX_FileAssets_StorageKey");

        // Hash index supports future de-duplication; filtered because Sha256 is optional in 2A.
        builder.HasIndex(x => x.Sha256)
            .HasFilter("[Sha256] IS NOT NULL")
            .HasDatabaseName("IX_FileAssets_Sha256");
    }
}
