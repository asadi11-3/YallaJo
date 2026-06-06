using ContentBlogs.Domain.Entities.Creators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentBlogs.Infrastructure.Persistence.Configurations;

public class CreatorFollowConfiguration : IEntityTypeConfiguration<CreatorFollow>
{
    public void Configure(EntityTypeBuilder<CreatorFollow> builder)
    {
        builder.ToTable("CreatorFollows", "content_blogs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.FollowerUserId).IsRequired();
        builder.Property(x => x.CreatorProfileId).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        // Hard-delete entity on unfollow: no IsDeleted of its own.
        // CCD-7: CreatorFollow is the required dependent of CreatorProfile, which has a
        // global query filter (!IsDeleted). Mirror the principal's filter via the
        // navigation so follows are hidden when the related profile is soft-deleted
        // (matches the BlogTour / BlogTranslation / BlogComment precedent in this module).
        // This is a model-level predicate only — no schema change, no migration.
        builder.HasQueryFilter(x => !x.CreatorProfile.IsDeleted);

        builder.HasIndex(x => new { x.FollowerUserId, x.CreatorProfileId }).IsUnique();
        builder.HasIndex(x => x.CreatorProfileId);
    }
}
