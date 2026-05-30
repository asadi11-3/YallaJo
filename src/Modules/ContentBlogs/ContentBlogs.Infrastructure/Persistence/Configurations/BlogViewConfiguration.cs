using ContentBlogs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentBlogs.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="BlogView"/> to <c>content_blogs.BlogViews</c>.  The unique
/// index on <c>(BlogId, ViewerHash)</c> enforces lifetime uniqueness — race
/// losers surface as <see cref="DbUpdateException"/> with SQL error 2601 / 2627
/// and are treated as <c>Counted = false</c> by the counter service.
/// </summary>
public sealed class BlogViewConfiguration : IEntityTypeConfiguration<BlogView>
{
    public void Configure(EntityTypeBuilder<BlogView> builder)
    {
        builder.ToTable("BlogViews");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.BlogId).IsRequired();

        builder.Property(x => x.ViewerHash)
            .IsRequired()
            .HasMaxLength(32)
            .HasColumnType("binary(32)");

        builder.Property(x => x.ViewerKind)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(x => x.ViewedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.BlogId, x.ViewerHash })
            .IsUnique()
            .HasDatabaseName("IX_BlogViews_BlogId_ViewerHash_Unique");
        builder.HasOne<Blog>()
            .WithMany()
            .HasForeignKey(x => x.BlogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
