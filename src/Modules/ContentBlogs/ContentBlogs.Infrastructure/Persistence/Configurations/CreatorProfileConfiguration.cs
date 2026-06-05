using ContentBlogs.Domain.Entities.Creators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentBlogs.Infrastructure.Persistence.Configurations;

public class CreatorProfileConfiguration : IEntityTypeConfiguration<CreatorProfile>
{
    public void Configure(EntityTypeBuilder<CreatorProfile> builder)
    {
        builder.ToTable("CreatorProfiles", "content_blogs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ApplicationId).IsRequired();

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Bio)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.Property(x => x.AvatarUrl)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.TrustTier)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.ArticleCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.TotalViewCount)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(x => x.TotalReactionCount)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(x => x.TotalCommentCount)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(x => x.FollowerCount)
            .IsRequired()
            .HasDefaultValue(0);

        // PublishedPostCount removed — merged into Blog entity (BlogCreatorPost-Merger plan)

        builder.Property(x => x.ReportCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.ReportRate)
            .IsRequired()
            .HasDefaultValue(0.0);

        builder.Property(x => x.EligibleForTier1)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.EligibleForTier2)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.LinkedProviderId).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.ApplicationId);
    }
}
