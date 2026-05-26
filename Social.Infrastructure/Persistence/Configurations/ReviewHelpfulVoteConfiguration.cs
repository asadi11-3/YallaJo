using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Social.Domain.Entities;

namespace Social.Infrastructure.Persistence.Configurations;

public sealed class ReviewHelpfulVoteConfiguration : IEntityTypeConfiguration<ReviewHelpfulVote>
{
    public void Configure(EntityTypeBuilder<ReviewHelpfulVote> builder)
    {
        builder.ToTable("ReviewHelpfulVotes", "social");
        builder.HasKey(x => new { x.ReviewId, x.UserId });
        builder.Property(x => x.ReviewId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.VotedAt).IsRequired();
        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_ReviewHelpfulVotes_UserId");
    }
}
