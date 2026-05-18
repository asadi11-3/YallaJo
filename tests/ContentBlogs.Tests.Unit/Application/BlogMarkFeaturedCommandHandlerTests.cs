using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Commands.Blog.MarkBlogAsFeatured;
using ContentBlogs.Application.Commands.Blog.MarkBlogAsUnfeatured;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Tests.Unit.Application;

public sealed class BlogMarkFeaturedCommandHandlerTests
{
    private static readonly Guid EnglishLanguageId = Guid.NewGuid();
    private const string ValidContent =
        "Petra is one of the most famous archaeological sites in the world, " +
        "carved into rose-coloured sandstone cliffs in the southern Jordanian " +
        "desert. Visiting at sunrise gives you an entirely different experience " +
        "compared to mid-day crowds.";

    // ── MarkBlogAsFeatured ───────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkBlogAsFeatured_ReturnsNotFound_WhenBlogMissing()
    {
        await using var db = NewDb();
        var cache = Substitute.For<HybridCache>();
        var handler = NewMarkAsFeaturedHandler(db, cache);

        var result = await handler.Handle(
            new MarkBlogAsFeaturedCommand(Guid.NewGuid(), [0xAA]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors[0].Code.Should().Be("Blog.NotFound");
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkBlogAsFeatured_ReturnsForbidden_WhenActorCannotManageAuthor()
    {
        await using var db = NewDb();
        var blog = NewPublishedBlog("hierarchy-feature");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = NewMarkAsFeaturedHandler(db, cache, guard: ForbiddenGuard());

        var result = await handler.Handle(
            new MarkBlogAsFeaturedCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkBlogAsFeatured_ReturnsForbidden_BeforeRowVersionConflict()
    {
        // No-leak invariant: forbidden hierarchy + stale RowVersion ⇒ Forbidden,
        // NOT Conflict.  Same property as all other Blog mutations.
        await using var db = NewDb();
        var blog = NewPublishedBlog("noleak-feature");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();
        SetRowVersion(blog, [0x10]);

        var handler = NewMarkAsFeaturedHandler(db, guard: ForbiddenGuard());
        var result = await handler.Handle(
            new MarkBlogAsFeaturedCommand(blog.Id, [0xFF]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");
        result.Errors[0].Code.Should().NotBe("Blog.ConcurrencyConflict");
    }

    [Fact]
    public async Task MarkBlogAsFeatured_ReturnsConflict_WhenRowVersionMismatch()
    {
        await using var db = NewDb();
        var blog = NewPublishedBlog("rv-feature");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();
        SetRowVersion(blog, [0x11]);

        var handler = NewMarkAsFeaturedHandler(db);
        var result = await handler.Handle(
            new MarkBlogAsFeaturedCommand(blog.Id, [0xFF]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.ConcurrencyConflict");
    }

    [Fact]
    public async Task MarkBlogAsFeatured_ReturnsConflict_WhenBlogIsDraft()
    {
        await using var db = NewDb();
        var blog = NewDraftBlog("draft-feature");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = NewMarkAsFeaturedHandler(db);
        var result = await handler.Handle(
            new MarkBlogAsFeaturedCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.InvalidTransition");
        var reloaded = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public async Task MarkBlogAsFeatured_ReturnsConflict_WhenBlogIsArchived()
    {
        await using var db = NewDb();
        var blog = NewPublishedBlog("archived-feature");
        blog.Archive(DateTime.UtcNow.AddMinutes(1));
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = NewMarkAsFeaturedHandler(db);
        var result = await handler.Handle(
            new MarkBlogAsFeaturedCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.InvalidTransition");
    }

    [Fact]
    public async Task MarkBlogAsFeatured_ReturnsConflict_WhenAlreadyFeatured()
    {
        await using var db = NewDb();
        var blog = NewPublishedBlog("already-featured");
        blog.MarkAsFeatured(DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = NewMarkAsFeaturedHandler(db);
        var result = await handler.Handle(
            new MarkBlogAsFeaturedCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.InvalidTransition");
    }

    [Fact]
    public async Task MarkBlogAsFeatured_ReturnsConflict_WhenAnotherBlogFeaturedInSameScope()
    {
        // D5/D6: per-PlaceId uniqueness.  Featuring a second blog with the same
        // PlaceId must fail with Blog.FeaturedConflict; no auto-replace.
        await using var db = NewDb();
        var placeId = Guid.NewGuid();

        var alreadyFeatured = NewPublishedBlog("already", placeId);
        alreadyFeatured.MarkAsFeatured(DateTime.UtcNow);
        var candidate = NewPublishedBlog("candidate", placeId);

        db.Blogs.AddRange(alreadyFeatured, candidate);
        await db.SaveChangesAsync();

        var handler = NewMarkAsFeaturedHandler(db);
        var result = await handler.Handle(
            new MarkBlogAsFeaturedCommand(candidate.Id, candidate.RowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.FeaturedConflict");

        // The candidate must remain not featured.
        var reloaded = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == candidate.Id);
        reloaded.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public async Task MarkBlogAsFeatured_AllowsFeatureInDifferentPlaceScope()
    {
        // Same uniqueness rule, opposite case: featuring in a DIFFERENT
        // PlaceId scope should succeed even when another scope has a featured
        // blog.  Pins the per-scope semantics of the uniqueness check.
        await using var db = NewDb();
        var placeA = Guid.NewGuid();
        var placeB = Guid.NewGuid();

        var aFeatured = NewPublishedBlog("a-featured", placeA);
        aFeatured.MarkAsFeatured(DateTime.UtcNow);
        var bCandidate = NewPublishedBlog("b-candidate", placeB);

        db.Blogs.AddRange(aFeatured, bCandidate);
        await db.SaveChangesAsync();

        var handler = NewMarkAsFeaturedHandler(db);
        var result = await handler.Handle(
            new MarkBlogAsFeaturedCommand(bCandidate.Id, bCandidate.RowVersion),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == bCandidate.Id);
        reloaded.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public async Task MarkBlogAsFeatured_SetsIsFeatured_WhenValid()
    {
        await using var db = NewDb();
        var blog = NewPublishedBlog("feature-valid");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = NewMarkAsFeaturedHandler(db);
        var result = await handler.Handle(
            new MarkBlogAsFeaturedCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public async Task MarkBlogAsFeatured_InvalidatesCache_AfterSuccess()
    {
        await using var db = NewDb();
        var blog = NewPublishedBlog("cache-feature");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = NewMarkAsFeaturedHandler(db, cache);

        await handler.Handle(
            new MarkBlogAsFeaturedCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogTag(blog.Id), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogSlugTag(blog.Slug), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogsListTag, Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.SitemapRenderedTag, Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.FeaturedBlogsTag, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkBlogAsFeatured_DoesNotInvalidateCache_OnFailure()
    {
        await using var db = NewDb();
        var cache = Substitute.For<HybridCache>();
        var handler = NewMarkAsFeaturedHandler(db, cache);

        // No blog seeded → NotFound path.
        await handler.Handle(
            new MarkBlogAsFeaturedCommand(Guid.NewGuid(), [0xAA]),
            CancellationToken.None);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    // ── MarkBlogAsUnfeatured ─────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkBlogAsUnfeatured_ReturnsNotFound_WhenBlogMissing()
    {
        await using var db = NewDb();
        var handler = NewMarkAsUnfeaturedHandler(db);

        var result = await handler.Handle(
            new MarkBlogAsUnfeaturedCommand(Guid.NewGuid(), [0xAA]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors[0].Code.Should().Be("Blog.NotFound");
    }

    [Fact]
    public async Task MarkBlogAsUnfeatured_ReturnsForbidden_WhenActorCannotManageAuthor()
    {
        await using var db = NewDb();
        var blog = NewPublishedBlog("hierarchy-unfeature");
        blog.MarkAsFeatured(DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = NewMarkAsUnfeaturedHandler(db, guard: ForbiddenGuard());
        var result = await handler.Handle(
            new MarkBlogAsUnfeaturedCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");
    }

    [Fact]
    public async Task MarkBlogAsUnfeatured_ReturnsForbidden_BeforeRowVersionConflict()
    {
        await using var db = NewDb();
        var blog = NewPublishedBlog("noleak-unfeature");
        blog.MarkAsFeatured(DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();
        SetRowVersion(blog, [0x10]);

        var handler = NewMarkAsUnfeaturedHandler(db, guard: ForbiddenGuard());
        var result = await handler.Handle(
            new MarkBlogAsUnfeaturedCommand(blog.Id, [0xFF]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().NotBe("Blog.ConcurrencyConflict");
    }

    [Fact]
    public async Task MarkBlogAsUnfeatured_ReturnsConflict_WhenRowVersionMismatch()
    {
        await using var db = NewDb();
        var blog = NewPublishedBlog("rv-unfeature");
        blog.MarkAsFeatured(DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();
        SetRowVersion(blog, [0x11]);

        var handler = NewMarkAsUnfeaturedHandler(db);
        var result = await handler.Handle(
            new MarkBlogAsUnfeaturedCommand(blog.Id, [0xFF]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.ConcurrencyConflict");
    }

    [Fact]
    public async Task MarkBlogAsUnfeatured_ReturnsConflict_WhenAlreadyNotFeatured()
    {
        await using var db = NewDb();
        var blog = NewPublishedBlog("not-featured");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = NewMarkAsUnfeaturedHandler(db);
        var result = await handler.Handle(
            new MarkBlogAsUnfeaturedCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.InvalidTransition");
    }

    [Fact]
    public async Task MarkBlogAsUnfeatured_FromFeaturedArchived_Succeeds()
    {
        // D2 regression at handler level: Unfeature must work when the
        // blog has been Archived after being Featured.
        await using var db = NewDb();
        var blog = NewPublishedBlog("featured-then-archived");
        blog.MarkAsFeatured(DateTime.UtcNow);
        blog.Archive(DateTime.UtcNow.AddMinutes(1));
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = NewMarkAsUnfeaturedHandler(db);
        var result = await handler.Handle(
            new MarkBlogAsUnfeaturedCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.IsFeatured.Should().BeFalse();
        reloaded.Status.Should().Be(BlogStatus.Archived,
            "Unfeature must NOT alter the lifecycle status");
    }

    [Fact]
    public async Task MarkBlogAsUnfeatured_ClearsIsFeatured_WhenValid()
    {
        await using var db = NewDb();
        var blog = NewPublishedBlog("unfeature-valid");
        blog.MarkAsFeatured(DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = NewMarkAsUnfeaturedHandler(db);
        var result = await handler.Handle(
            new MarkBlogAsUnfeaturedCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public async Task MarkBlogAsUnfeatured_InvalidatesCache_AfterSuccess()
    {
        await using var db = NewDb();
        var blog = NewPublishedBlog("cache-unfeature");
        blog.MarkAsFeatured(DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = NewMarkAsUnfeaturedHandler(db, cache);

        await handler.Handle(
            new MarkBlogAsUnfeaturedCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogTag(blog.Id), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogSlugTag(blog.Slug), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogsListTag, Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.SitemapRenderedTag, Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.FeaturedBlogsTag, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkBlogAsUnfeatured_DoesNotInvalidateCache_OnFailure()
    {
        await using var db = NewDb();
        var cache = Substitute.For<HybridCache>();
        var handler = NewMarkAsUnfeaturedHandler(db, cache);

        await handler.Handle(
            new MarkBlogAsUnfeaturedCommand(Guid.NewGuid(), [0xAA]),
            CancellationToken.None);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static ContentBlogsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-feature-{Guid.NewGuid():N}")
            .Options;
        return new ContentBlogsDbContext(options);
    }

    private static Blog NewDraftBlog(string slug, Guid? placeId = null) =>
        Blog.Create(
            title:            $"Blog {slug}",
            slug:             slug,
            content:          ValidContent,
            authorId:         Guid.NewGuid(),
            sourceLanguageId: EnglishLanguageId,
            utcNow:           DateTime.UtcNow,
            placeId:          placeId);

    private static Blog NewPublishedBlog(string slug, Guid? placeId = null)
    {
        var blog = NewDraftBlog(slug, placeId);
        blog.Publish(DateTime.UtcNow);
        return blog;
    }

    private static IBlogAuthorHierarchyGuard PermissiveGuard()
    {
        var g = Substitute.For<IBlogAuthorHierarchyGuard>();
        g.EnsureCanManageBlogOwnedByAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        return g;
    }

    private static IBlogAuthorHierarchyGuard ForbiddenGuard()
    {
        var g = Substitute.For<IBlogAuthorHierarchyGuard>();
        g.EnsureCanManageBlogOwnedByAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(
                new Error(
                    "Blog.AuthorHierarchyForbidden",
                    "You cannot manage content created by a user at the same or higher privilege level."),
                Outcome.Forbidden));
        return g;
    }

    private static IContentBlogsUnitOfWork UnitOfWork(ContentBlogsDbContext db)
    {
        var uow = Substitute.For<IContentBlogsUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci => db.SaveChangesAsync(ci.Arg<CancellationToken>()));
        return uow;
    }

    private static MarkBlogAsFeaturedCommandHandler NewMarkAsFeaturedHandler(
        ContentBlogsDbContext db,
        HybridCache? cache = null,
        IBlogAuthorHierarchyGuard? guard = null) =>
        new(
            blogRepository:       new BlogRepository(db),
            authorHierarchyGuard: guard ?? PermissiveGuard(),
            unitOfWork:           UnitOfWork(db),
            cache:                cache ?? Substitute.For<HybridCache>(),
            logger:               NullLogger<MarkBlogAsFeaturedCommandHandler>.Instance);

    private static MarkBlogAsUnfeaturedCommandHandler NewMarkAsUnfeaturedHandler(
        ContentBlogsDbContext db,
        HybridCache? cache = null,
        IBlogAuthorHierarchyGuard? guard = null) =>
        new(
            blogRepository:       new BlogRepository(db),
            authorHierarchyGuard: guard ?? PermissiveGuard(),
            unitOfWork:           UnitOfWork(db),
            cache:                cache ?? Substitute.For<HybridCache>(),
            logger:               NullLogger<MarkBlogAsUnfeaturedCommandHandler>.Instance);

    private static void SetRowVersion(Blog blog, byte[] value)
    {
        var prop = typeof(YallaJo.SharedKernel.Domain.Entities.AuditableEntity)
            .GetProperty(
                nameof(YallaJo.SharedKernel.Domain.Entities.AuditableEntity.RowVersion),
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
        prop!.SetValue(blog, value);
    }
}
