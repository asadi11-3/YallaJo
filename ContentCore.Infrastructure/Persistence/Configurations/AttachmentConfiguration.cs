using ContentCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentCore.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments", "content_core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.EntityId).IsRequired();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Url)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(2048);

        builder.Property(x => x.ThumbnailUrl)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(2048);

        builder.Property(x => x.OriginalFileName)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(500);

        builder.Property(x => x.MimeType)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(100);

        builder.Property(x => x.FileSize).IsRequired(false);
        builder.Property(x => x.Width).IsRequired(false);
        builder.Property(x => x.Height).IsRequired(false);
        builder.Property(x => x.DurationSeconds).IsRequired(false);

        builder.Property(x => x.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.Iv)
            .IsRequired(false)
            .HasColumnType("varbinary(max)");

        builder.Property(x => x.Hmac)
            .IsRequired(false)
            .HasColumnType("varbinary(max)");

        builder.Property(x => x.IsMarkedForDeletion)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.UploadedAt).IsRequired();
        builder.Property(x => x.UploadedByUserId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}
