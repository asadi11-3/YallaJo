using ContentCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentCore.Infrastructure.Persistence.Configurations;

public class EntityImageConfiguration : IEntityTypeConfiguration<EntityImage>
{
    public void Configure(EntityTypeBuilder<EntityImage> builder)
    {
        builder.ToTable("EntityImages", "content_core");

        builder.HasKey(x => new { x.EntityType, x.EntityId, x.AttachmentId, x.ImageSize });

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.AttachmentId).IsRequired();

        builder.Property(x => x.ImageSize)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.SortOrder).HasDefaultValue(0);
        builder.Property(x => x.IsPrimary).IsRequired().HasDefaultValue(false);

        builder.HasOne(x => x.Attachment)
            .WithMany()
            .HasForeignKey(x => x.AttachmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
