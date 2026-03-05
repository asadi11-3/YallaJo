using ContentSeo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentSeo.Infrastructure.Persistence.Configurations;

public class RedirectConfiguration : IEntityTypeConfiguration<Redirect>
{
    public void Configure(EntityTypeBuilder<Redirect> builder)
    {
        builder.ToTable("Redirects", "content_seo");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.OldUrl)
            .IsRequired()
            .HasMaxLength(2048)
            .IsUnicode(false);

        builder.Property(x => x.NewUrl)
            .IsRequired()
            .HasMaxLength(2048)
            .IsUnicode(false);

        builder.Property(x => x.StatusCode)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.HitCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.OldUrl)
            .IsUnique()
            .HasFilter("[IsActive] = 1");
    }
}
