using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ContentBlogs.Tests.Unit.Infrastructure;

/// <summary>
/// CCD-7: verifies the matching query filter added to <c>CreatorFollow</c>
/// (<c>HasQueryFilter(x =&gt; !x.CreatorProfile.IsDeleted)</c>). Follows must be hidden
/// from normal queries when the related <see cref="CreatorProfile"/> is soft-deleted
/// (self-deactivated), while the rows still physically exist (recoverable via
/// <c>IgnoreQueryFilters()</c>). This mirrors the BlogTour / BlogComment precedent and
/// requires no schema change.
/// </summary>
public sealed class CreatorFollowQueryFilterTests
{
    private static ContentBlogsDbContext NewDb() =>
        new(new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-creator-follow-filter-{Guid.NewGuid():N}")
            .Options);

    private static CreatorProfile NewProfile()
    {
        var result = CreatorProfile.Create(
            userId: Guid.NewGuid(),
            applicationId: Guid.NewGuid(),
            slug: $"creator-{Guid.NewGuid():N}",
            displayName: "Creator",
            bio: null,
            avatarUrl: null);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }

    private static CreatorFollow NewFollow(Guid creatorProfileId)
    {
        var result = CreatorFollow.Create(Guid.NewGuid(), creatorProfileId);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }

    [Fact]
    public async Task Follows_are_visible_for_an_active_profile()
    {
        await using var db = NewDb();
        var profile = NewProfile();
        db.CreatorProfiles.Add(profile);
        db.CreatorFollows.Add(NewFollow(profile.Id));
        db.CreatorFollows.Add(NewFollow(profile.Id));
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        (await db.CreatorFollows.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Follows_are_hidden_when_the_profile_is_soft_deleted()
    {
        await using var db = NewDb();
        var profile = NewProfile();
        db.CreatorProfiles.Add(profile);
        db.CreatorFollows.Add(NewFollow(profile.Id));
        db.CreatorFollows.Add(NewFollow(profile.Id));
        await db.SaveChangesAsync();

        // Self-deactivate → soft delete (IsDeleted = true).
        profile.Deactivate(DateTime.UtcNow);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        // The matching filter hides the follows of a soft-deleted profile…
        (await db.CreatorFollows.CountAsync()).Should().Be(0);
        // …but the rows still physically exist.
        (await db.CreatorFollows.IgnoreQueryFilters().CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Repository_count_and_list_respect_the_filter_for_a_soft_deleted_profile()
    {
        await using var db = NewDb();
        var profile = NewProfile();
        db.CreatorProfiles.Add(profile);
        db.CreatorFollows.Add(NewFollow(profile.Id));
        await db.SaveChangesAsync();

        var repo = new CreatorFollowRepository(db);
        (await repo.CountByCreatorProfileIdAsync(profile.Id)).Should().Be(1);
        (await repo.GetFollowerUserIdsAsync(profile.Id, page: 1, pageSize: 20)).Should().HaveCount(1);

        profile.Deactivate(DateTime.UtcNow);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repoAfter = new CreatorFollowRepository(db);
        (await repoAfter.CountByCreatorProfileIdAsync(profile.Id)).Should().Be(0);
        (await repoAfter.GetFollowerUserIdsAsync(profile.Id, page: 1, pageSize: 20)).Should().BeEmpty();
    }
}
