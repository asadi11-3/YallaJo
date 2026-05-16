using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Commands.Blog.LinkBlogTours;
using ContentBlogs.Application.Commands.Blog.UnlinkBlogFromTour;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Repositories;
using ContentTours.Contracts.Tours;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Tests.Unit.Application;

/// <summary>
/// Handler-level tests for Blog ↔ Tour link/unlink lifecycle.  Mirrors the
/// no-leak ordering pattern of <c>BlogLifecycleCommandHandlerTests</c>:
/// NotFound → Hierarchy → RowVersion → business — none of which leak data
/// about content the actor cannot manage.
/// </summary>
public sealed class BlogTourLinkCommandHandlerTests
{
    private static readonly Guid EnglishLanguageId = Guid.NewGuid();
    private const string ValidContent =
        "Petra is one of the most famous archaeological sites in the world, " +
        "carved into rose-coloured sandstone cliffs in the southern Jordanian " +
        "desert.  Visiting at sunrise gives you an entirely different experience " +
        "compared to mid-day crowds.";

    // ── LinkBlogTours ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LinkBlogTours_ReturnsNotFound_WhenBlogMissing()
    {
        await using var db = NewDb();
        var cache = Substitute.For<HybridCache>();
        var handler = NewLinkHandler(db, cache);

        var result = await handler.Handle(
            new LinkBlogToursCommand(
                BlogId:     Guid.NewGuid(),
                RowVersion: [0xAA],
                Tours:      [new LinkBlogTourItem(Guid.NewGuid())]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors[0].Code.Should().Be("Blog.NotFound");
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LinkBlogTours_ReturnsForbidden_WhenActorCannotManageBlogAuthor()
    {
        await using var db = NewDb();
        var blog = NewBlog("hierarchy-link");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = NewLinkHandler(db, cache, guard: ForbiddenGuard());

        var result = await handler.Handle(
            new LinkBlogToursCommand(blog.Id, blog.RowVersion,
                [new LinkBlogTourItem(Guid.NewGuid())]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LinkBlogTours_ReturnsConflict_WhenRowVersionMismatch()
    {
        await using var db = NewDb();
        var blog = NewBlog("rv-link");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();
        SetRowVersion(blog, [0x10]);

        var handler = NewLinkHandler(db);
        var result = await handler.Handle(
            new LinkBlogToursCommand(blog.Id, [0xFF],
                [new LinkBlogTourItem(Guid.NewGuid())]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.ConcurrencyConflict");
    }

    [Fact]
    public async Task LinkBlogTours_ReturnsUnprocessable_WhenTourMissing()
    {
        await using var db = NewDb();
        var blog = NewBlog("tour-missing");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var existence = Substitute.For<ITourExistenceService>();
        existence.GetStatusAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(TourExistenceStatus.NotFound);

        var handler = NewLinkHandler(db, existence: existence);
        var result = await handler.Handle(
            new LinkBlogToursCommand(blog.Id, blog.RowVersion,
                [new LinkBlogTourItem(Guid.NewGuid())]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.UnprocessableEntity);
        result.Errors[0].Code.Should().Be("Blog.TourNotFound");
    }

    [Fact]
    public async Task LinkBlogTours_ReturnsUnprocessable_WhenTourDeleted()
    {
        await using var db = NewDb();
        var blog = NewBlog("tour-deleted");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var existence = Substitute.For<ITourExistenceService>();
        existence.GetStatusAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(TourExistenceStatus.Deleted);

        var handler = NewLinkHandler(db, existence: existence);
        var result = await handler.Handle(
            new LinkBlogToursCommand(blog.Id, blog.RowVersion,
                [new LinkBlogTourItem(Guid.NewGuid())]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.UnprocessableEntity);
        result.Errors[0].Code.Should().Be("Blog.TourDeleted");
    }

    [Fact]
    public async Task LinkBlogTours_ReturnsConflict_WhenMaxToursExceeded()
    {
        await using var db = NewDb();
        var blog = NewBlog("max-tours");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        // Seed 9 existing links so adding 2 more would push past the cap (10).
        for (var i = 0; i < 9; i++)
        {
            db.BlogTours.Add(BlogTour.Create(blog.Id, Guid.NewGuid(), i));
        }
        await db.SaveChangesAsync();

        var handler = NewLinkHandler(db);
        var result = await handler.Handle(
            new LinkBlogToursCommand(blog.Id, blog.RowVersion,
                [
                    new LinkBlogTourItem(Guid.NewGuid()),
                    new LinkBlogTourItem(Guid.NewGuid()),
                ]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.MaxToursExceeded");
    }

    [Fact]
    public async Task LinkBlogTours_IsIdempotent_ForAlreadyLinkedTour()
    {
        await using var db = NewDb();
        var blog = NewBlog("idempotent");
        var existingTourId = Guid.NewGuid();
        db.Blogs.Add(blog);
        db.BlogTours.Add(BlogTour.Create(blog.Id, existingTourId, 0));
        await db.SaveChangesAsync();

        var handler = NewLinkHandler(db);
        // Request includes the already-linked tour + one new tour.
        var newTourId = Guid.NewGuid();
        var result = await handler.Handle(
            new LinkBlogToursCommand(blog.Id, blog.RowVersion,
                [
                    new LinkBlogTourItem(existingTourId),
                    new LinkBlogTourItem(newTourId),
                ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var linkedIds = await db.BlogTours.AsNoTracking()
            .Where(bt => bt.BlogId == blog.Id)
            .Select(bt => bt.TourId)
            .ToListAsync();
        linkedIds.Should().BeEquivalentTo(new[] { existingTourId, newTourId },
            "duplicates are silently skipped — only the new tour is added");
    }

    [Fact]
    public async Task LinkBlogTours_AddsNewLinks_WhenValid()
    {
        await using var db = NewDb();
        var blog = NewBlog("valid");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();

        var handler = NewLinkHandler(db);
        var result = await handler.Handle(
            new LinkBlogToursCommand(blog.Id, blog.RowVersion,
                [
                    new LinkBlogTourItem(t1, SortOrder: 5),
                    new LinkBlogTourItem(t2),
                ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var rows = await db.BlogTours.AsNoTracking()
            .Where(bt => bt.BlogId == blog.Id)
            .ToListAsync();
        rows.Should().HaveCount(2);
        rows.Single(r => r.TourId == t1).SortOrder.Should().Be(5);
    }

    [Fact]
    public async Task LinkBlogTours_InvalidatesCache_AfterSuccess()
    {
        await using var db = NewDb();
        var blog = NewBlog("cache-link");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = NewLinkHandler(db, cache);

        await handler.Handle(
            new LinkBlogToursCommand(blog.Id, blog.RowVersion,
                [new LinkBlogTourItem(Guid.NewGuid())]),
            CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogTag(blog.Id), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogToursTag(blog.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LinkBlogTours_DoesNotInvalidateCache_OnFailure()
    {
        await using var db = NewDb();
        var cache = Substitute.For<HybridCache>();
        var handler = NewLinkHandler(db, cache);

        // No blog seeded ⇒ NotFound path.
        await handler.Handle(
            new LinkBlogToursCommand(Guid.NewGuid(), [0xAA],
                [new LinkBlogTourItem(Guid.NewGuid())]),
            CancellationToken.None);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    // ── UnlinkBlogFromTour ────────────────────────────────────────────────────

    [Fact]
    public async Task UnlinkBlogFromTour_ReturnsNotFound_WhenBlogMissing()
    {
        await using var db = NewDb();
        var handler = NewUnlinkHandler(db);

        var result = await handler.Handle(
            new UnlinkBlogFromTourCommand(Guid.NewGuid(), Guid.NewGuid(), [0xAA]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors[0].Code.Should().Be("Blog.NotFound");
    }

    [Fact]
    public async Task UnlinkBlogFromTour_ReturnsForbidden_WhenActorCannotManageBlogAuthor()
    {
        await using var db = NewDb();
        var blog = NewBlog("hier-unlink");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = NewUnlinkHandler(db, guard: ForbiddenGuard());
        var result = await handler.Handle(
            new UnlinkBlogFromTourCommand(blog.Id, Guid.NewGuid(), blog.RowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");
    }

    [Fact]
    public async Task UnlinkBlogFromTour_ReturnsConflict_WhenRowVersionMismatch()
    {
        await using var db = NewDb();
        var blog = NewBlog("rv-unlink");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();
        SetRowVersion(blog, [0x11]);

        var handler = NewUnlinkHandler(db);
        var result = await handler.Handle(
            new UnlinkBlogFromTourCommand(blog.Id, Guid.NewGuid(), [0xFF]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.ConcurrencyConflict");
    }

    [Fact]
    public async Task UnlinkBlogFromTour_RemovesLink_WhenValid()
    {
        await using var db = NewDb();
        var blog = NewBlog("unlink-valid");
        var tourId = Guid.NewGuid();
        db.Blogs.Add(blog);
        db.BlogTours.Add(BlogTour.Create(blog.Id, tourId, 0));
        await db.SaveChangesAsync();

        var handler = NewUnlinkHandler(db);
        var result = await handler.Handle(
            new UnlinkBlogFromTourCommand(blog.Id, tourId, blog.RowVersion),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.BlogTours.AnyAsync(bt => bt.BlogId == blog.Id && bt.TourId == tourId))
            .Should().BeFalse();
    }

    [Fact]
    public async Task UnlinkBlogFromTour_ReturnsNotFound_WhenLinkMissing()
    {
        // D1 of the plan: non-idempotent unlink.
        await using var db = NewDb();
        var blog = NewBlog("unlink-missing");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = NewUnlinkHandler(db);
        var result = await handler.Handle(
            new UnlinkBlogFromTourCommand(blog.Id, Guid.NewGuid(), blog.RowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors[0].Code.Should().Be("Blog.TourLinkNotFound");
    }

    [Fact]
    public async Task UnlinkBlogFromTour_InvalidatesCache_AfterSuccess()
    {
        await using var db = NewDb();
        var blog = NewBlog("unlink-cache");
        var tourId = Guid.NewGuid();
        db.Blogs.Add(blog);
        db.BlogTours.Add(BlogTour.Create(blog.Id, tourId, 0));
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = NewUnlinkHandler(db, cache);
        await handler.Handle(
            new UnlinkBlogFromTourCommand(blog.Id, tourId, blog.RowVersion),
            CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogTag(blog.Id), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogToursTag(blog.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnlinkBlogFromTour_DoesNotInvalidateCache_OnFailure()
    {
        await using var db = NewDb();
        var cache = Substitute.For<HybridCache>();
        var handler = NewUnlinkHandler(db, cache);

        // No blog seeded ⇒ NotFound.
        await handler.Handle(
            new UnlinkBlogFromTourCommand(Guid.NewGuid(), Guid.NewGuid(), [0xAA]),
            CancellationToken.None);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    // ── No-leak ordering ─────────────────────────────────────────────────────

    [Fact]
    public async Task ForbiddenHierarchy_DoesNotCheckRowVersionFirst_ForBlogTourLinks()
    {
        // Stale RowVersion combined with forbidden hierarchy MUST surface as
        // Forbidden, NOT Conflict — otherwise an attacker could detect
        // concurrency state of content they have no right to manage.
        await using var db = NewDb();
        var blog = NewBlog("noleak-link");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();
        SetRowVersion(blog, [0x10]);

        var handler = NewLinkHandler(db, guard: ForbiddenGuard());
        var result = await handler.Handle(
            new LinkBlogToursCommand(blog.Id, [0xFF],
                [new LinkBlogTourItem(Guid.NewGuid())]),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");
        result.Errors[0].Code.Should().NotBe("Blog.ConcurrencyConflict");
    }

    [Fact]
    public async Task ForbiddenHierarchy_DoesNotSaveChanges_ForBlogTourLinks()
    {
        await using var db = NewDb();
        var blog = NewBlog("noleak-save");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var guard = ForbiddenGuard();

        await NewLinkHandler(db, guard: guard).Handle(
            new LinkBlogToursCommand(blog.Id, blog.RowVersion,
                [new LinkBlogTourItem(Guid.NewGuid())]),
            CancellationToken.None);

        await NewUnlinkHandler(db, guard: guard).Handle(
            new UnlinkBlogFromTourCommand(blog.Id, Guid.NewGuid(), blog.RowVersion),
            CancellationToken.None);

        (await db.BlogTours.AnyAsync()).Should().BeFalse(
            "no link mutation persisted on the forbidden path");
    }

    [Fact]
    public async Task ForbiddenHierarchy_DoesNotInvalidateCache_ForBlogTourLinks()
    {
        await using var db = NewDb();
        var blog = NewBlog("noleak-cache");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var guard = ForbiddenGuard();

        await NewLinkHandler(db, cache, guard: guard).Handle(
            new LinkBlogToursCommand(blog.Id, blog.RowVersion,
                [new LinkBlogTourItem(Guid.NewGuid())]),
            CancellationToken.None);
        await NewUnlinkHandler(db, cache, guard: guard).Handle(
            new UnlinkBlogFromTourCommand(blog.Id, Guid.NewGuid(), blog.RowVersion),
            CancellationToken.None);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static ContentBlogsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-tour-links-{Guid.NewGuid():N}")
            .Options;
        return new ContentBlogsDbContext(options);
    }

    private static Blog NewBlog(string slug) =>
        Blog.Create(
            title:            $"Blog {slug}",
            slug:             slug,
            content:          ValidContent,
            authorId:         Guid.NewGuid(),
            sourceLanguageId: EnglishLanguageId,
            utcNow:           DateTime.UtcNow);

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

    private static ITourExistenceService ActiveExistence()
    {
        var s = Substitute.For<ITourExistenceService>();
        s.GetStatusAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(TourExistenceStatus.Active);
        return s;
    }

    private static IContentBlogsUnitOfWork UnitOfWork(ContentBlogsDbContext db)
    {
        var uow = Substitute.For<IContentBlogsUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci => db.SaveChangesAsync(ci.Arg<CancellationToken>()));
        return uow;
    }

    private static LinkBlogToursCommandHandler NewLinkHandler(
        ContentBlogsDbContext db,
        HybridCache? cache = null,
        IBlogAuthorHierarchyGuard? guard = null,
        ITourExistenceService? existence = null) =>
        new(
            blogRepository:       new BlogRepository(db),
            blogTourRepository:   new BlogTourRepository(db),
            authorHierarchyGuard: guard ?? PermissiveGuard(),
            tourExistenceService: existence ?? ActiveExistence(),
            unitOfWork:           UnitOfWork(db),
            cache:                cache ?? Substitute.For<HybridCache>(),
            logger:               NullLogger<LinkBlogToursCommandHandler>.Instance);

    private static UnlinkBlogFromTourCommandHandler NewUnlinkHandler(
        ContentBlogsDbContext db,
        HybridCache? cache = null,
        IBlogAuthorHierarchyGuard? guard = null) =>
        new(
            blogRepository:       new BlogRepository(db),
            blogTourRepository:   new BlogTourRepository(db),
            authorHierarchyGuard: guard ?? PermissiveGuard(),
            unitOfWork:           UnitOfWork(db),
            cache:                cache ?? Substitute.For<HybridCache>(),
            logger:               NullLogger<UnlinkBlogFromTourCommandHandler>.Instance);

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
