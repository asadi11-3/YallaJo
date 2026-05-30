using ContentBlogs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentBlogs.Infrastructure.Persistence.Configurations;

public class BlogCommentReactionConfiguration : IEntityTypeConfiguration<BlogCommentReaction>
{
    public void Configure(EntityTypeBuilder<BlogCommentReaction> builder)
    {
        builder.ToTable("BlogCommentReactions", "content_blogs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CommentId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.ReactionType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.BlogComment)
            .WithMany(x => x.Reactions)
            .HasForeignKey(x => x.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x =>
            !x.BlogComment.IsDeleted &&
            !x.BlogComment.Blog.IsDeleted);

        builder.HasIndex(x => new { x.CommentId, x.UserId }).IsUnique();
    }
}
