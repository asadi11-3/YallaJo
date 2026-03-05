using ContentBlogs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentBlogs.Infrastructure.Persistence.Configurations;

public class BlogTourConfiguration : IEntityTypeConfiguration<BlogTour>
{
    public void Configure(EntityTypeBuilder<BlogTour> builder)
    {
        builder.ToTable("BlogTours", "content_blogs");

        builder.HasKey(x => new { x.BlogId, x.TourId });

        builder.Property(x => x.BlogId).IsRequired();
        builder.Property(x => x.TourId).IsRequired();

        builder.Property(x => x.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(x => x.Blog)
            .WithMany()
            .HasForeignKey(x => x.BlogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.Blog.IsDeleted);
    }
}
