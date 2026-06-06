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

        // Hard-delete entity: it has no IsDeleted of its own. However, the principal
        // CreatorProfile defines a soft-delete query filter (!IsDeleted). To avoid EF's
        // "required end of relationship may be filtered out" warning (linkid=2131316),
        // mirror the principal's filter via the navigation so follows of a soft-deleted
        // creator are filtered out together with their parent profile.
        builder.HasQueryFilter(x => !x.CreatorProfile.IsDeleted);

        builder.HasIndex(x => new { x.FollowerUserId, x.CreatorProfileId }).IsUnique();
        builder.HasIndex(x => x.CreatorProfileId);
    }
}
