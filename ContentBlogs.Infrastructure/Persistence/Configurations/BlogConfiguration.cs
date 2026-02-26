using ContentBlogs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentBlogs.Infrastructure.Persistence.Configurations;

public class BlogConfiguration : IEntityTypeConfiguration<Blog>
{
    public void Configure(EntityTypeBuilder<Blog> builder)
    {
        builder.ToTable("Blogs", "content_blogs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Summary)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(x => x.AuthorId).IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(0);

        builder.Property(x => x.IsFeatured)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.ViewCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.ReadTimeMinutes).IsRequired(false);

        builder.Property(x => x.MetaTitle)
            .IsRequired(false)
            .HasMaxLength(200);

        builder.Property(x => x.MetaDescription)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.PublishedAt).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.BlogTranslations)
            .WithOne(x => x.Blog)
            .HasForeignKey(x => x.BlogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.BlogComments)
            .WithOne(x => x.Blog)
            .HasForeignKey(x => x.BlogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
