using ContentBlogs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentBlogs.Infrastructure.Persistence.Configurations;

public class BlogTranslationConfiguration : IEntityTypeConfiguration<BlogTranslation>
{
    public void Configure(EntityTypeBuilder<BlogTranslation> builder)
    {
        builder.ToTable("BlogTranslations", "content_blogs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.BlogId).IsRequired();
        builder.Property(x => x.LanguageId).IsRequired();

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Summary)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.Blog)
            .WithMany(x => x.BlogTranslations)
            .HasForeignKey(x => x.BlogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.BlogId, x.LanguageId }).IsUnique();
        builder.HasIndex(x => x.LanguageId);
    }
}
