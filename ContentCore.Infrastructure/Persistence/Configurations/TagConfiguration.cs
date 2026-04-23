using ContentCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentCore.Infrastructure.Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags", "content_core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .IsUnicode(true)
            .HasMaxLength(100);

        builder.Property(x => x.Slug)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(100);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.SourceLanguageCode)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(10)
            .HasDefaultValue("en");

        // CreatedAt, UpdatedAt, RowVersion, IsDeleted, DeletedAt are managed by
        // AuditableEntity's shared EF base configuration in SharedKernel. Do not re-configure here.

        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
