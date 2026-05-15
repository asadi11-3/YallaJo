using ContentBlogs.Domain.Events;
using ContentBlogs.Infrastructure.EventHandlers;
using ContentBlogs.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Tests.Unit.Infrastructure;

/// <summary>
/// Phase-1 outbox-only domain-event handlers (no SaveChanges, no cache,
/// no orchestration).  Each test asserts that the handler stages exactly one
/// <see cref="OutboxMessage"/> with the integration-event logical name from
/// <c>IntegrationEventTypeRegistry</c>, and that the message has NOT been
/// persisted (it sits in the ChangeTracker as <see cref="EntityState.Added"/>).
/// </summary>
public sealed class BlogOutboxDomainEventHandlerTests
{
    private static ContentBlogsDbContext NewInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-outbox-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ContentBlogsDbContext(options);
    }

    private static OutboxMessage SingleStaged(ContentBlogsDbContext db) =>
        db.ChangeTracker
          .Entries<OutboxMessage>()
          .Where(e => e.State == EntityState.Added)
          .Select(e => e.Entity)
          .Should().ContainSingle().Subject;

    private static void AssertNotPersisted(ContentBlogsDbContext db)
    {
        // Handler must NOT call SaveChangesAsync — the row is staged but not yet
        // persisted to the in-memory database.
        db.OutboxMessages.AsNoTracking().Any().Should().BeFalse(
            "domain-event handlers MUST NOT call SaveChangesAsync; persistence is " +
            "the responsibility of the surrounding UnitOfWork.SaveChangesAsync.");
    }

    // ── BlogCreated ───────────────────────────────────────────────────────────

    [Fact]
    public async Task BlogCreatedDomainEventHandler_WritesBlogCreatedOutboxMessage()
    {
        await using var db = NewInMemoryDbContext();
        var handler = new BlogCreatedDomainEventHandler(
            db, Substitute.For<ILogger<BlogCreatedDomainEventHandler>>());

        var blogId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var evt = new BlogCreatedDomainEvent(
            BlogId:           blogId,
            Slug:             "petra-sunrise",
            Title:            "Petra Sunrise",
            AuthorId:         authorId,
            SourceLanguageId: Guid.NewGuid(),
            PlaceId:          null,
            CreatedAtUtc:     DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<BlogCreatedDomainEvent>(evt),
            CancellationToken.None);

        var staged = SingleStaged(db);
        staged.Type.Should().Be("content-blogs.blog.created.v1");
        staged.Content.Should().Contain(blogId.ToString());
        staged.Content.Should().Contain("petra-sunrise");
        staged.Content.Should().Contain("Petra Sunrise");
        staged.Content.Should().Contain(authorId.ToString());

        AssertNotPersisted(db);
    }

    // ── BlogUpdated ───────────────────────────────────────────────────────────

    [Fact]
    public async Task BlogUpdatedDomainEventHandler_WritesBlogUpdatedOutboxMessage()
    {
        await using var db = NewInMemoryDbContext();
        var handler = new BlogUpdatedDomainEventHandler(
            db, Substitute.For<ILogger<BlogUpdatedDomainEventHandler>>());

        var blogId = Guid.NewGuid();
        var evt = new BlogUpdatedDomainEvent(
            BlogId:        blogId,
            OldSlug:       "petra-old",
            NewSlug:       "petra-new",
            FieldsChanged: new[] { "Title", "Slug" },
            UpdatedAtUtc:  DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<BlogUpdatedDomainEvent>(evt),
            CancellationToken.None);

        var staged = SingleStaged(db);
        staged.Type.Should().Be("content-blogs.blog.updated.v1");
        staged.Content.Should().Contain(blogId.ToString());
        staged.Content.Should().Contain("petra-old");
        staged.Content.Should().Contain("petra-new");
        staged.Content.Should().Contain("Title");
        staged.Content.Should().Contain("Slug");

        AssertNotPersisted(db);
    }

    // ── BlogDeleted ───────────────────────────────────────────────────────────

    [Fact]
    public async Task BlogDeletedDomainEventHandler_WritesBlogDeletedOutboxMessage()
    {
        await using var db = NewInMemoryDbContext();
        var handler = new BlogDeletedDomainEventHandler(
            db, Substitute.For<ILogger<BlogDeletedDomainEventHandler>>());

        var blogId = Guid.NewGuid();
        var evt = new BlogDeletedDomainEvent(
            BlogId:       blogId,
            Slug:         "petra-deleted",
            DeletedAtUtc: DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<BlogDeletedDomainEvent>(evt),
            CancellationToken.None);

        var staged = SingleStaged(db);
        staged.Type.Should().Be("content-blogs.blog.deleted.v1");
        staged.Content.Should().Contain(blogId.ToString());
        staged.Content.Should().Contain("petra-deleted");

        AssertNotPersisted(db);
    }

    // ── BlogPublished ─────────────────────────────────────────────────────────

    [Fact]
    public async Task BlogPublishedDomainEventHandler_WritesBlogPublishedOutboxMessage()
    {
        await using var db = NewInMemoryDbContext();
        var handler = new BlogPublishedDomainEventHandler(
            db, Substitute.For<ILogger<BlogPublishedDomainEventHandler>>());

        var blogId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var placeId = Guid.NewGuid();
        var evt = new BlogPublishedDomainEvent(
            BlogId:         blogId,
            Slug:           "petra-published",
            Title:          "Petra Published",
            AuthorId:       authorId,
            PlaceId:        placeId,
            PublishedAtUtc: DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<BlogPublishedDomainEvent>(evt),
            CancellationToken.None);

        var staged = SingleStaged(db);
        staged.Type.Should().Be("content-blogs.blog.published.v1");
        staged.Content.Should().Contain(blogId.ToString());
        staged.Content.Should().Contain("petra-published");
        staged.Content.Should().Contain(authorId.ToString());
        staged.Content.Should().Contain(placeId.ToString());

        AssertNotPersisted(db);
    }

    // ── BlogUnpublished ───────────────────────────────────────────────────────

    [Fact]
    public async Task BlogUnpublishedDomainEventHandler_WritesBlogUnpublishedOutboxMessage()
    {
        await using var db = NewInMemoryDbContext();
        var handler = new BlogUnpublishedDomainEventHandler(
            db, Substitute.For<ILogger<BlogUnpublishedDomainEventHandler>>());

        var blogId = Guid.NewGuid();
        var evt = new BlogUnpublishedDomainEvent(
            BlogId:           blogId,
            Slug:             "petra-unpublished",
            UnpublishedAtUtc: DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<BlogUnpublishedDomainEvent>(evt),
            CancellationToken.None);

        var staged = SingleStaged(db);
        staged.Type.Should().Be("content-blogs.blog.unpublished.v1");
        staged.Content.Should().Contain(blogId.ToString());
        staged.Content.Should().Contain("petra-unpublished");

        AssertNotPersisted(db);
    }

    // ── BlogArchived ──────────────────────────────────────────────────────────

    [Fact]
    public async Task BlogArchivedDomainEventHandler_WritesBlogArchivedOutboxMessage()
    {
        await using var db = NewInMemoryDbContext();
        var handler = new BlogArchivedDomainEventHandler(
            db, Substitute.For<ILogger<BlogArchivedDomainEventHandler>>());

        var blogId = Guid.NewGuid();
        var evt = new BlogArchivedDomainEvent(
            BlogId:        blogId,
            Slug:          "petra-archived",
            ArchivedAtUtc: DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<BlogArchivedDomainEvent>(evt),
            CancellationToken.None);

        var staged = SingleStaged(db);
        staged.Type.Should().Be("content-blogs.blog.archived.v1");
        staged.Content.Should().Contain(blogId.ToString());
        staged.Content.Should().Contain("petra-archived");

        AssertNotPersisted(db);
    }
}
