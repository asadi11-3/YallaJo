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
/// Phase-3 outbox-only domain-event handlers for Blog ↔ Tour link/unlink.
/// Each handler stages exactly one <see cref="OutboxMessage"/> with the
/// registered logical name and never calls <c>SaveChangesAsync</c>.
/// </summary>
public sealed class BlogTourOutboxDomainEventHandlerTests
{
    private static ContentBlogsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-tour-outbox-{Guid.NewGuid():N}")
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

    [Fact]
    public async Task BlogTourLinkedDomainEventHandler_WritesBlogTourLinkedOutboxMessage()
    {
        await using var db = NewDb();
        var handler = new BlogTourLinkedDomainEventHandler(
            db, Substitute.For<ILogger<BlogTourLinkedDomainEventHandler>>());

        var blogId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var evt = new BlogTourLinkedDomainEvent(blogId, tourId, DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<BlogTourLinkedDomainEvent>(evt),
            CancellationToken.None);

        var staged = SingleStaged(db);
        staged.Type.Should().Be("content-blogs.blog-tour.linked.v1");
        staged.Content.Should().Contain(blogId.ToString());
        staged.Content.Should().Contain(tourId.ToString());

        // Handler MUST NOT call SaveChangesAsync — the row is staged in
        // ChangeTracker but not yet persisted.
        db.OutboxMessages.AsNoTracking().Any().Should().BeFalse();
    }

    [Fact]
    public async Task BlogTourUnlinkedDomainEventHandler_WritesBlogTourUnlinkedOutboxMessage()
    {
        await using var db = NewDb();
        var handler = new BlogTourUnlinkedDomainEventHandler(
            db, Substitute.For<ILogger<BlogTourUnlinkedDomainEventHandler>>());

        var blogId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var evt = new BlogTourUnlinkedDomainEvent(blogId, tourId, DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<BlogTourUnlinkedDomainEvent>(evt),
            CancellationToken.None);

        var staged = SingleStaged(db);
        staged.Type.Should().Be("content-blogs.blog-tour.unlinked.v1");
        staged.Content.Should().Contain(blogId.ToString());
        staged.Content.Should().Contain(tourId.ToString());

        db.OutboxMessages.AsNoTracking().Any().Should().BeFalse();
    }
}
