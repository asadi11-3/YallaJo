using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Commands.Blog.RestoreBlog;
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

/// <summary>
/// Handler-level tests for the RestoreBlog admin flow.
/// Mirrors the no-leak ordering pattern of the lifecycle handler tests:
/// NotFound → AlreadyNotDeleted → Hierarchy → RowVersion → FeaturedConflict
/// → domain → SaveChanges → cache.
/// </summary>
public sealed class BlogRestoreCommandHandlerTests
{
    private static readonly Guid EnglishLanguageId = Guid.NewGuid();
    private const string ValidContent =
        "Petra is one of the most famous archaeological sites in the world, " +
        "carved into rose-coloured sandstone cliffs in the southern Jordanian " +
        "desert. Visiting at sunrise gives you an entirely different experience " +
        "compared to mid-day crowds.";

    [Fact]
    public async Task RestoreBlog_ReturnsNotFound_WhenMissingEvenIncludingDeleted()
    {
        await using var db = NewDb();
        var cache = Substitute.For<HybridCache>();
        var handler = NewHandler(db, cache);

        var result = await handler.Handle(
            new RestoreBlogCommand(Guid.NewGuid(), [0xAA]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors[0].Code.Should().Be("Blog.NotFound");
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoreBlog_ReturnsConflict_WhenBlogIsNotDeleted()
    {
        await using var db = NewDb();
        var blog = NewDraftBlog("not-deleted");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = NewHandler(db, cache);

        var result = await handler.Handle(
            new RestoreBlogCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.InvalidTransition");
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoreBlog_ReturnsForbidden_WhenActorCannotManageAuthor()
    {
        await using var db = NewDb();
        var blog = NewDeletedBlog("hierarchy-restore");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = NewHandler(db, cache, guard: ForbiddenGuard());

        var result = await handler.Handle(
            new RestoreBlogCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoreBlog_ReturnsForbidden_BeforeRowVersionConflict()
    {
        // No-leak invariant: forbidden hierarchy + stale RowVersion ⇒ Forbidden,
        // NOT Conflict.  Same property as all other Blog mutations.
        await using var db = NewDb();
        var blog = NewDeletedBlog("noleak-restore");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();
        SetRowVersion(blog, [0x10]);

        var handler = NewHandler(db, guard: ForbiddenGuard());
        var result = await handler.Handle(
            new RestoreBlogCommand(blog.Id, [0xFF]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");
        result.Errors[0].Code.Should().NotBe("Blog.ConcurrencyConflict");
    }

    [Fact]
    public async Task RestoreBlog_ReturnsConflict_WhenRowVersionMismatch()
    {
        await using var db = NewDb();
        var blog = NewDeletedBlog("rv-restore");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();
        SetRowVersion(blog, [0x11]);

        var handler = NewHandler(db);
        var result = await handler.Handle(
            new RestoreBlogCommand(blog.Id, [0xFF]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.ConcurrencyConflict");
    }

    [Fact]
    public async Task RestoreBlog_ReturnsConflict_WhenFeaturedConflictExists()
    {
        // D2: per-PlaceId featured uniqueness re-check on restore.
        await using var db = NewDb();
        var placeId = Guid.NewGuid();

        // A: deleted, was Featured in placeId
        var deletedFeatured = NewPublishedBlog("deleted-featured", placeId);
        deletedFeatured.Feature(Guid.Empty, DateTime.UtcNow);
        deletedFeatured.Delete(DateTime.UtcNow.AddMinutes(1));

        // B: still live, also Featured in same placeId — occupies the slot
        var liveFeatured = NewPublishedBlog("live-featured", placeId);
        liveFeatured.Feature(Guid.Empty, DateTime.UtcNow);

        db.Blogs.AddRange(deletedFeatured, liveFeatured);
        await db.SaveChangesAsync();

        var handler = NewHandler(db);
        var result = await handler.Handle(
            new RestoreBlogCommand(deletedFeatured.Id, deletedFeatured.RowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.FeaturedConflict");

        // The deleted blog must remain deleted.
        var reloaded = await db.Blogs.IgnoreQueryFilters().AsNoTracking()
            .FirstAsync(b => b.Id == deletedFeatured.Id);
        reloaded.IsDeleted.Should().BeTrue(
            "restore was blocked by featured-conflict; the blog must stay deleted");
    }

    [Fact]
    public async Task RestoreBlog_RestoresDeletedBlog_WhenValid()
    {
        await using var db = NewDb();
        var blog = NewDeletedBlog("restore-valid");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = NewHandler(db);
        var result = await handler.Handle(
            new RestoreBlogCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.IsDeleted.Should().BeFalse();
        reloaded.DeletedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(BlogStatus.Draft)]
    [InlineData(BlogStatus.Published)]
    [InlineData(BlogStatus.Archived)]
    public async Task RestoreBlog_KeepsOriginalStatus_WhenValid(BlogStatus targetStatus)
    {
        await using var db = NewDb();
        var blog = NewDraftBlog($"restore-status-{targetStatus}");
        switch (targetStatus)
        {
            case BlogStatus.Published:
                blog.Publish(DateTime.UtcNow);
                break;
            case BlogStatus.Archived:
                blog.Publish(DateTime.UtcNow);
                blog.Archive(DateTime.UtcNow.AddMinutes(1));
                break;
            case BlogStatus.Draft:
            default:
                break;
        }
        blog.Delete(DateTime.UtcNow.AddMinutes(2));
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = NewHandler(db);
        var result = await handler.Handle(
            new RestoreBlogCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.Status.Should().Be(targetStatus,
            "Restore preserves Status — see plan D1");
    }

    [Fact]
    public async Task RestoreBlog_InvalidatesCache_AfterSuccess_BasicTags()
    {
        await using var db = NewDb();
        var blog = NewDeletedBlog("cache-restore-basic");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = NewHandler(db, cache);

        var result = await handler.Handle(
            new RestoreBlogCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogTag(blog.Id), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogSlugTag(blog.Slug), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogsListTag, Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.SitemapRenderedTag, Arg.Any<CancellationToken>());

        // No featured / no place ⇒ neither conditional tag should fire.
        await cache.DidNotReceive().RemoveByTagAsync(
            ContentBlogsCacheKeys.FeaturedBlogsTag, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoreBlog_InvalidatesCache_AfterSuccess_FeaturedAndPlaceTags()
    {
        await using var db = NewDb();
        var placeId = Guid.NewGuid();
        var blog = NewPublishedBlog("cache-restore-featured", placeId);
        blog.Feature(Guid.Empty, DateTime.UtcNow);
        blog.Delete(DateTime.UtcNow.AddMinutes(1));
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = NewHandler(db, cache);

        var result = await handler.Handle(
            new RestoreBlogCommand(blog.Id, blog.RowVersion),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.FeaturedBlogsTag, Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogPlaceTag(placeId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoreBlog_DoesNotInvalidateCache_OnFailure()
    {
        await using var db = NewDb();
        var cache = Substitute.For<HybridCache>();
        var handler = NewHandler(db, cache);

        // NotFound path
        await handler.Handle(
            new RestoreBlogCommand(Guid.NewGuid(), [0xAA]),
            CancellationToken.None);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static ContentBlogsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-restore-{Guid.NewGuid():N}")
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

    private static Blog NewDeletedBlog(string slug, Guid? placeId = null)
    {
        var blog = NewDraftBlog(slug, placeId);
        blog.Delete(DateTime.UtcNow);
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

    private static RestoreBlogCommandHandler NewHandler(
        ContentBlogsDbContext db,
        HybridCache? cache = null,
        IBlogAuthorHierarchyGuard? guard = null) =>
        new(
            blogRepository:       new BlogRepository(db),
            authorHierarchyGuard: guard ?? PermissiveGuard(),
            unitOfWork:           UnitOfWork(db),
            cache:                cache ?? Substitute.For<HybridCache>(),
            logger:               NullLogger<RestoreBlogCommandHandler>.Instance);

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
