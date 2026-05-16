using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Infrastructure.EventHandlers;
using ContentBlogs.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Repositories;
using ContentPlaces.Contracts.IntegrationEvents;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Inbox;

namespace ContentBlogs.Tests.Unit.Infrastructure;

/// <summary>
/// Tests for the ContentBlogs consumer that clears dead Place references
/// when a Place is deleted in ContentPlaces.
/// <para>See CONTENTBLOGS-PLACE-DELETED-CONSUMER-PLAN-001 §5/§6 for the
/// approved handler flow and cache plan.</para>
/// </summary>
public sealed class PlaceDeletedIntegrationEventHandlerTests
{
    private const string ValidContent =
        "Petra is one of the most famous archaeological sites in the world, " +
        "carved into rose-coloured sandstone cliffs in the southern Jordanian " +
        "desert. Visiting at sunrise gives you an entirely different experience " +
        "compared to mid-day crowds.";

    // ── Mutation correctness ──────────────────────────────────────────────────

    [Fact]
    public async Task PlaceDeletedConsumer_UnlinksAllBlogsForPlace()
    {
        await using var db = NewDb();
        var placeA = Guid.NewGuid();
        var placeB = Guid.NewGuid();
        var a1 = NewDraftBlog("a1", placeA);
        var a2 = NewPublishedBlog("a2", placeA);
        var a3 = NewArchivedBlog("a3", placeA);
        var b1 = NewDraftBlog("b1", placeB);
        var b2 = NewPublishedBlog("b2", placeB);
        db.Blogs.AddRange(a1, a2, a3, b1, b2);
        await db.SaveChangesAsync();

        var handler = NewHandler(db);
        await handler.Handle(NewNotification(placeA), CancellationToken.None);

        var reloaded = await db.Blogs.AsNoTracking().ToListAsync();
        reloaded.Where(b => b.Slug.StartsWith("a")).Should()
            .OnlyContain(b => b.PlaceId == null,
                "all placeA blogs (Draft, Published, Archived) must be unlinked");
        reloaded.Where(b => b.Slug.StartsWith("b")).Should()
            .OnlyContain(b => b.PlaceId == placeB,
                "blogs scoped to other places must remain untouched");
    }

    [Fact]
    public async Task PlaceDeletedConsumer_DoesNotTouchBlogsForOtherPlaces()
    {
        await using var db = NewDb();
        var placeA = Guid.NewGuid();
        var placeB = Guid.NewGuid();
        var blogA = NewPublishedBlog("a", placeA);
        var blogB = NewPublishedBlog("b", placeB);
        db.Blogs.AddRange(blogA, blogB);
        await db.SaveChangesAsync();
        var blogBUpdatedAtBefore = blogB.UpdatedAt;

        var handler = NewHandler(db);
        await handler.Handle(NewNotification(placeA), CancellationToken.None);

        var reloadedB = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == blogB.Id);
        reloadedB.PlaceId.Should().Be(placeB);
        reloadedB.UpdatedAt.Should().Be(blogBUpdatedAtBefore,
            "non-matching blogs must not have UpdatedAt bumped");
    }

    [Fact]
    public async Task PlaceDeletedConsumer_DoesNotTouchDeletedBlogs()
    {
        await using var db = NewDb();
        var placeA = Guid.NewGuid();
        var liveBlog = NewPublishedBlog("live", placeA);
        var deletedBlog = NewDraftBlog("deleted", placeA);
        deletedBlog.Delete(DateTime.UtcNow);
        db.Blogs.AddRange(liveBlog, deletedBlog);
        await db.SaveChangesAsync();

        var handler = NewHandler(db);
        await handler.Handle(NewNotification(placeA), CancellationToken.None);

        var reloadedLive = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == liveBlog.Id);
        reloadedLive.PlaceId.Should().BeNull("live blog must be unlinked");

        // Deleted blog: bypass query filter to reload and inspect.
        var reloadedDeleted = await db.Blogs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(b => b.Id == deletedBlog.Id);
        reloadedDeleted.PlaceId.Should().Be(placeA,
            "soft-deleted blogs are frozen — their old PlaceId must be preserved (plan D2)");
    }

    [Fact]
    public async Task PlaceDeletedConsumer_PreservesStatusPublishedAtIsFeatured()
    {
        await using var db = NewDb();
        var placeA = Guid.NewGuid();
        var blog = NewPublishedBlog("featured", placeA);
        var publishedAtBefore = blog.PublishedAt;
        blog.MarkAsFeatured(DateTime.UtcNow.AddMinutes(1));
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = NewHandler(db);
        await handler.Handle(NewNotification(placeA), CancellationToken.None);

        var reloaded = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.PlaceId.Should().BeNull();
        reloaded.Status.Should().Be(BlogStatus.Published,
            "consumer must not transition editorial state");
        reloaded.PublishedAt.Should().Be(publishedAtBefore,
            "PublishedAt must not be touched");
        reloaded.IsFeatured.Should().BeTrue(
            "IsFeatured must not be cleared by the consumer");
    }

    // ── Idempotency (inbox dedup) ─────────────────────────────────────────────

    [Fact]
    public async Task PlaceDeletedConsumer_IsIdempotent_WhenInboxAlreadyProcessed()
    {
        await using var db = NewDb();
        var placeA = Guid.NewGuid();
        var blog = NewPublishedBlog("a", placeA);
        db.Blogs.Add(blog);

        var messageId = Guid.NewGuid();
        // Pre-seed the inbox row as if a previous delivery had succeeded.
        db.Set<InboxMessage>().Add(InboxMessage.Create(messageId));
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = NewHandler(db, cache: cache);

        await handler.Handle(
            new IntegrationEventNotification<PlaceDeletedIntegrationEvent>(
                messageId, new PlaceDeletedIntegrationEvent(placeA)),
            CancellationToken.None);

        var reloaded = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.PlaceId.Should().Be(placeA,
            "the message was already processed — no mutation must occur");
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());

        // No second inbox row was added (still exactly one).
        var inboxCount = await db.Set<InboxMessage>().CountAsync(m => m.Id == messageId);
        inboxCount.Should().Be(1);
    }

    [Fact]
    public async Task PlaceDeletedConsumer_MarksInboxProcessed_AfterSuccess()
    {
        await using var db = NewDb();
        var placeA = Guid.NewGuid();
        db.Blogs.Add(NewPublishedBlog("a", placeA));
        await db.SaveChangesAsync();

        var messageId = Guid.NewGuid();
        var handler = NewHandler(db);

        await handler.Handle(
            new IntegrationEventNotification<PlaceDeletedIntegrationEvent>(
                messageId, new PlaceDeletedIntegrationEvent(placeA)),
            CancellationToken.None);

        var inboxRow = await db.Set<InboxMessage>().AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == messageId);
        inboxRow.Should().NotBeNull(
            "the handler must mark the message processed so it is not redelivered");
    }

    // ── SaveChanges call count ────────────────────────────────────────────────

    [Fact]
    public async Task PlaceDeletedConsumer_SavesOnce_ForMultipleBlogs()
    {
        await using var db = NewDb();
        var placeA = Guid.NewGuid();
        db.Blogs.AddRange(
            NewPublishedBlog("a1", placeA),
            NewPublishedBlog("a2", placeA),
            NewPublishedBlog("a3", placeA),
            NewArchivedBlog("a4", placeA),
            NewDraftBlog("a5", placeA));
        await db.SaveChangesAsync();

        var uow = new CountingUnitOfWork(db);
        var handler = NewHandler(db, uow: uow);

        await handler.Handle(NewNotification(placeA), CancellationToken.None);

        uow.SaveChangesCallCount.Should().Be(1,
            "the handler must invoke SaveChangesAsync exactly once per delivery");
    }

    // ── Cache invalidation ────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceDeletedConsumer_InvalidatesCache_AfterSuccess_WithFeaturedBlog()
    {
        await using var db = NewDb();
        var placeA = Guid.NewGuid();
        var a1 = NewPublishedBlog("a1", placeA);
        a1.MarkAsFeatured(DateTime.UtcNow);
        var a2 = NewPublishedBlog("a2", placeA);
        db.Blogs.AddRange(a1, a2);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = NewHandler(db, cache: cache);

        await handler.Handle(NewNotification(placeA), CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogsListTag, Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogPlaceTag(placeA), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.SitemapRenderedTag, Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogTag(a1.Id), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogTag(a2.Id), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogSlugTag(a1.Slug), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogSlugTag(a2.Slug), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.FeaturedBlogsTag, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceDeletedConsumer_InvalidatesCache_AfterSuccess_WithoutFeaturedBlog()
    {
        await using var db = NewDb();
        var placeA = Guid.NewGuid();
        db.Blogs.Add(NewPublishedBlog("a1", placeA)); // not featured
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = NewHandler(db, cache: cache);

        await handler.Handle(NewNotification(placeA), CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogsListTag, Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogPlaceTag(placeA), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(
            ContentBlogsCacheKeys.FeaturedBlogsTag, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceDeletedConsumer_DoesNotInvalidateCache_OnFailure()
    {
        await using var db = NewDb();
        var placeA = Guid.NewGuid();
        db.Blogs.Add(NewPublishedBlog("a", placeA));
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var failingUow = Substitute.For<IContentBlogsUnitOfWork>();
        failingUow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new DbUpdateException("simulated SQL error"));

        var handler = NewHandler(db, cache: cache, uow: failingUow);

        var act = async () =>
            await handler.Handle(NewNotification(placeA), CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>();
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceDeletedConsumer_HandlesNoAffectedBlogs()
    {
        await using var db = NewDb();
        var placeA = Guid.NewGuid();
        var placeB = Guid.NewGuid();
        db.Blogs.Add(NewPublishedBlog("b", placeB)); // wrong place
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var uow = new CountingUnitOfWork(db);
        var messageId = Guid.NewGuid();
        var handler = NewHandler(db, cache: cache, uow: uow);

        await handler.Handle(
            new IntegrationEventNotification<PlaceDeletedIntegrationEvent>(
                messageId, new PlaceDeletedIntegrationEvent(placeA)),
            CancellationToken.None);

        // Inbox row was written (idempotency safeguard for no-op messages).
        var inboxRow = await db.Set<InboxMessage>().AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == messageId);
        inboxRow.Should().NotBeNull();

        // Exactly one SaveChanges (the inbox-only save).
        uow.SaveChangesCallCount.Should().Be(1);

        // NO cache invalidation — nothing changed.
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceDeletedConsumer_IgnoresEmptyGuidPayload()
    {
        await using var db = NewDb();
        var placeA = Guid.NewGuid();
        db.Blogs.Add(NewPublishedBlog("a", placeA));
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var messageId = Guid.NewGuid();
        var handler = NewHandler(db, cache: cache);

        await handler.Handle(
            new IntegrationEventNotification<PlaceDeletedIntegrationEvent>(
                messageId, new PlaceDeletedIntegrationEvent(Guid.Empty)),
            CancellationToken.None);

        // No blogs mutated.
        var reloaded = await db.Blogs.AsNoTracking().ToListAsync();
        reloaded.Should().OnlyContain(b => b.PlaceId == placeA);

        // Poison message marked processed (so it doesn't keep being redelivered).
        var inboxRow = await db.Set<InboxMessage>().AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == messageId);
        inboxRow.Should().NotBeNull(
            "even an empty-Guid payload must be marked processed to avoid endless redelivery");

        // No cache invalidation.
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static ContentBlogsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-place-deleted-{Guid.NewGuid():N}")
            .Options;
        return new ContentBlogsDbContext(options);
    }

    private static IntegrationEventNotification<PlaceDeletedIntegrationEvent>
        NewNotification(Guid placeId) =>
            new(Guid.NewGuid(), new PlaceDeletedIntegrationEvent(placeId));

    private static Blog NewDraftBlog(string slug, Guid? placeId = null) =>
        Blog.Create(
            title:            $"Blog {slug}",
            slug:             slug,
            content:          ValidContent,
            authorId:         Guid.NewGuid(),
            sourceLanguageId: Guid.NewGuid(),
            utcNow:           DateTime.UtcNow,
            placeId:          placeId);

    private static Blog NewPublishedBlog(string slug, Guid? placeId = null)
    {
        var blog = NewDraftBlog(slug, placeId);
        blog.Publish(DateTime.UtcNow);
        return blog;
    }

    private static Blog NewArchivedBlog(string slug, Guid? placeId = null)
    {
        var blog = NewPublishedBlog(slug, placeId);
        blog.Archive(DateTime.UtcNow.AddMinutes(1));
        return blog;
    }

    private static IContentBlogsUnitOfWork DefaultUnitOfWork(ContentBlogsDbContext db)
    {
        var uow = Substitute.For<IContentBlogsUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci => db.SaveChangesAsync(ci.Arg<CancellationToken>()));
        return uow;
    }

    /// <summary>
    /// Test-local inbox store that exactly mirrors the production
    /// <c>ContentBlogsInboxStore</c> behavior (HasBeenProcessed + MarkAsProcessed
    /// against <c>InboxMessage</c> rows in the same DbContext).  We don't
    /// reference the production type directly because it is <c>internal</c>
    /// and the test project does not have <c>InternalsVisibleTo</c>.
    /// </summary>
    private static IContentBlogsInboxStore InboxStore(ContentBlogsDbContext db) =>
        new TestInboxStore(db);

    private sealed class TestInboxStore(ContentBlogsDbContext context) : IContentBlogsInboxStore
    {
        public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken ct = default)
            => context.Set<InboxMessage>().AnyAsync(m => m.Id == messageId, ct);

        public void MarkAsProcessed(Guid messageId)
            => context.Set<InboxMessage>().Add(InboxMessage.Create(messageId));
    }

    private static PlaceDeletedIntegrationEventHandler NewHandler(
        ContentBlogsDbContext db,
        HybridCache? cache = null,
        IContentBlogsUnitOfWork? uow = null) =>
        new(
            blogRepository: new BlogRepository(db),
            unitOfWork:     uow ?? DefaultUnitOfWork(db),
            inboxStore:     InboxStore(db),
            cache:          cache ?? Substitute.For<HybridCache>(),
            logger:         NullLogger<PlaceDeletedIntegrationEventHandler>.Instance);

    /// <summary>
    /// UoW spy that counts SaveChangesAsync calls and delegates to the real
    /// in-memory DbContext.  Used by <c>SavesOnce_ForMultipleBlogs</c> and
    /// <c>HandlesNoAffectedBlogs</c>.
    /// </summary>
    private sealed class CountingUnitOfWork(ContentBlogsDbContext db) : IContentBlogsUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            SaveChangesCallCount++;
            return db.SaveChangesAsync(ct);
        }
    }
}
