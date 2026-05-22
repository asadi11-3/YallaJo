using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentBlogs.Infrastructure.Persistence.Configurations;

public class CreatorPostConfiguration : IEntityTypeConfiguration<CreatorPost>
{
    public void Configure(EntityTypeBuilder<CreatorPost> builder)
    {
        builder.ToTable("CreatorPosts", "content_blogs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CreatorProfileId).IsRequired();

        builder.Property(x => x.PostType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Excerpt)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Body)
            .IsRequired(false);

        builder.Property(x => x.LanguageId).IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.SubmittedAt).IsRequired(false);
        builder.Property(x => x.ReviewedAt).IsRequired(false);
        builder.Property(x => x.ReviewedByAdminId).IsRequired(false);
        builder.Property(x => x.PublishedAt).IsRequired(false);

        builder.Property(x => x.RejectionReason)
            .IsRequired(false)
            .HasMaxLength(1000);

        // Featuring
        builder.Property(x => x.IsFeatured).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.FeaturedAt).IsRequired(false);
        builder.Property(x => x.FeaturedByAdminId).IsRequired(false);
        builder.Property(x => x.FeaturedUntil).IsRequired(false);

        // Disclosure
        builder.Property(x => x.IsSponsored).IsRequired().HasDefaultValue(false);

        builder.OwnsMany(x => x.DisclosedTargets, dt =>
        {
            dt.ToJson();
            dt.Property(d => d.EntityType).HasMaxLength(100);
            dt.Property(d => d.RelationKind).HasConversion<int>();
        });

        // Stats
        builder.Property(x => x.ViewCount).IsRequired().HasDefaultValue(0L);
        builder.Property(x => x.ReactionCount).IsRequired().HasDefaultValue(0L);
        builder.Property(x => x.CommentCount).IsRequired().HasDefaultValue(0L);
        builder.Property(x => x.ReportCount).IsRequired().HasDefaultValue(0L);

        // JSON columns
        builder.Property(x => x.TaggedEntityIds)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(x => x.TaggedEntityTypes)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(x => x.NicheIds)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(x => x.FreeTags)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(x => x.PlaceRegionIds)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(x => x.TypeSpecificDataJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        // Auditable
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);

        // Indexes
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.CreatorProfileId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.IsFeatured, x.FeaturedUntil })
            .HasFilter("[IsFeatured] = 1");
        builder.HasIndex(x => new { x.Status, x.SubmittedAt })
            .HasFilter("[Status] = 1"); // PendingReview
    }
}
