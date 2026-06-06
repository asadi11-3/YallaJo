using ContentBlogs.Application.Queries.Blog.GetCreatorBlogsBySlug;
using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Application.Queries.Creator.GetCreatorProfileBySlug;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Tests.Unit.Application;

public sealed class CreatorProfileVisibilityQueryTests
{
    private static readonly Guid EnglishLanguageId = Guid.NewGuid();
    private const string ValidContent =
        "Petra is one of the most famous archaeological sites in the world, carved " +
        "into rose-coloured sandstone cliffs in the southern Jordanian desert.";

    private static ContentBlogsDbContext NewDb() =>
        new(new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-creator-visibility-{Guid.NewGuid():N}")
            .Options);

    private static CreatorProfile NewProfile(string slug)
    {
        var result = CreatorProfile.Create(
            userId: Guid.NewGuid(),
            applicationId: Guid.NewGuid(),
            slug: slug,
            displayName: $"Creator {slug}",
            bio: "Travel writer",
            avatarUrl: null);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }

    // ── GetCreatorProfileBySlug ────────────────────────────────────────────────

    [Fact]
    public async Task Profile_lookup_returns_active_creator()
    {
        await using var db = NewDb();
        var profile = NewProfile("active-creator"); // Created in Active state by factory.
        db.CreatorProfiles.Add(profile);
        await db.SaveChangesAsync();

        var handler = new GetCreatorProfileBySlugQueryHandler(
            new CreatorProfileRepository(db),
            NullLogger<GetCreatorProfileBySlugQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetCreatorProfileBySlugQuery("active-creator"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Slug.Should().Be("active-creator");
        // Gap 4: the by-slug endpoint now returns the public-safe PublicCreatorProfileDto
        // (only an Active profile is ever returned, so there is no Status field to leak).
        result.Value.DisplayName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Public_profile_dto_does_not_expose_internal_fields()
    {
        // Gap 4: the public-safe PublicCreatorProfileDto must not carry UserId, internal
        // Status, LinkedProviderId, or CreatedAt. Asserted at the type level (compile-time
        // contract) so the privacy guarantee cannot regress unnoticed.
        var publicProps = typeof(PublicCreatorProfileDto)
            .GetProperties()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        publicProps.Should().NotContain("UserId");
        publicProps.Should().NotContain("Status");
        publicProps.Should().NotContain("LinkedProviderId");
        publicProps.Should().NotContain("CreatedAt");

        // …but it must still carry the public-safe display fields.
        publicProps.Should().Contain("Slug");
        publicProps.Should().Contain("DisplayName");
        publicProps.Should().Contain("FollowerCount");
    }

    [Fact]
    public async Task Profile_lookup_returns_not_found_for_suspended_creator()
    {
        await using var db = NewDb();
        var profile = NewProfile("suspended-creator");
        profile.Suspend(Guid.NewGuid(), "Policy violation under review");
        db.CreatorProfiles.Add(profile);
        await db.SaveChangesAsync();

        var handler = new GetCreatorProfileBySlugQueryHandler(
            new CreatorProfileRepository(db),
            NullLogger<GetCreatorProfileBySlugQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetCreatorProfileBySlugQuery("suspended-creator"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Error!.Code.Should().Be("CreatorProfile.NotFound");
    }

    [Fact]
    public async Task Profile_lookup_returns_not_found_for_deactivated_creator()
    {
        await using var db = NewDb();
        var profile = NewProfile("deactivated-creator");
        profile.Deactivate(DateTime.UtcNow); // soft-delete (IsDeleted = true)
        db.CreatorProfiles.Add(profile);
        await db.SaveChangesAsync();

        var handler = new GetCreatorProfileBySlugQueryHandler(
            new CreatorProfileRepository(db),
            NullLogger<GetCreatorProfileBySlugQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetCreatorProfileBySlugQuery("deactivated-creator"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    // ── GetCreatorBlogsBySlug ──────────────────────────────────────────────────

    [Fact]
    public async Task Creator_blogs_lookup_returns_blogs_for_active_creator()
    {
        await using var db = NewDb();
        var profile = NewProfile("active-author");
        db.CreatorProfiles.Add(profile);

        var blog = Blog.CreateByCreator(
            title: "Exploring Petra",
            slug: "exploring-petra",
            content: ValidContent,
            authorId: profile.UserId,
            creatorProfileId: profile.Id,
            sourceLanguageId: EnglishLanguageId,
            utcNow: DateTime.UtcNow);
        blog.Publish(DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = new GetCreatorBlogsBySlugQueryHandler(
            new CreatorProfileRepository(db),
            new BlogRepository(db));

        var result = await handler.Handle(
            new GetCreatorBlogsBySlugQuery("active-author", 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle()
            .Which.Slug.Should().Be("exploring-petra");
    }

    [Fact]
    public async Task Creator_blogs_lookup_returns_not_found_for_suspended_creator()
    {
        await using var db = NewDb();
        var profile = NewProfile("suspended-author");
        var blog = Blog.CreateByCreator(
            title: "Exploring Petra",
            slug: "exploring-petra-2",
            content: ValidContent,
            authorId: profile.UserId,
            creatorProfileId: profile.Id,
            sourceLanguageId: EnglishLanguageId,
            utcNow: DateTime.UtcNow);
        blog.Publish(DateTime.UtcNow);
        profile.Suspend(Guid.NewGuid(), "Policy violation under review");
        db.CreatorProfiles.Add(profile);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = new GetCreatorBlogsBySlugQueryHandler(
            new CreatorProfileRepository(db),
            new BlogRepository(db));

        var result = await handler.Handle(
            new GetCreatorBlogsBySlugQuery("suspended-author", 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Error!.Code.Should().Be("CreatorProfile.NotFound");
    }

    [Fact]
    public async Task Creator_blogs_lookup_returns_not_found_for_deactivated_creator()
    {
        await using var db = NewDb();
        var profile = NewProfile("deactivated-author");
        profile.Deactivate(DateTime.UtcNow);
        db.CreatorProfiles.Add(profile);
        await db.SaveChangesAsync();

        var handler = new GetCreatorBlogsBySlugQueryHandler(
            new CreatorProfileRepository(db),
            new BlogRepository(db));

        var result = await handler.Handle(
            new GetCreatorBlogsBySlugQuery("deactivated-author", 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
    }
}
