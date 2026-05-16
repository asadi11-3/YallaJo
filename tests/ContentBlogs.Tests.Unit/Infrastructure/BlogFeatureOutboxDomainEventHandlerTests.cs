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
/// Outbox-only domain-event handlers for Blog Feature / Unfeatured lifecycle.
/// Each handler stages exactly one <see cref="OutboxMessage"/> with the
/// registered logical name and never calls <c>SaveChangesAsync</c>.
/// </summary>
public sealed class BlogFeatureOutboxDomainEventHandlerTests
{
    private static ContentBlogsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-feature-outbox-{Guid.NewGuid():N}")
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
    public async Task BlogFeaturedDomainEventHandler_WritesBlogFeaturedOutboxMessage()
    {
        await using var db = NewDb();
        var handler = new BlogFeaturedDomainEventHandler(
            db, Substitute.For<ILogger<BlogFeaturedDomainEventHandler>>());

        var blogId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var placeId = Guid.NewGuid();
        var evt = new BlogFeaturedDomainEvent(
            BlogId:        blogId,
            Slug:          "amazing-petra-trip",
            Title:         "Amazing Petra Trip",
            AuthorId:      authorId,
            PlaceId:       placeId,
            FeaturedAtUtc: DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<BlogFeaturedDomainEvent>(evt),
            CancellationToken.None);

        var staged = SingleStaged(db);
        staged.Type.Should().Be("content-blogs.blog.featured.v1");
        staged.Content.Should().Contain(blogId.ToString());
        staged.Content.Should().Contain("amazing-petra-trip");

        // Handler MUST NOT call SaveChangesAsync — the row is staged in
        // ChangeTracker but not yet persisted.
        db.OutboxMessages.AsNoTracking().Any().Should().BeFalse();
    }

    [Fact]
    public async Task BlogUnfeaturedDomainEventHandler_WritesBlogUnfeaturedOutboxMessage()
    {
        await using var db = NewDb();
        var handler = new BlogUnfeaturedDomainEventHandler(
            db, Substitute.For<ILogger<BlogUnfeaturedDomainEventHandler>>());

        var blogId = Guid.NewGuid();
        var placeId = Guid.NewGuid();
        var evt = new BlogUnfeaturedDomainEvent(
            BlogId:          blogId,
            Slug:            "amazing-petra-trip",
            PlaceId:         placeId,
            UnfeaturedAtUtc: DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<BlogUnfeaturedDomainEvent>(evt),
            CancellationToken.None);

        var staged = SingleStaged(db);
        staged.Type.Should().Be("content-blogs.blog.unfeatured.v1");
        staged.Content.Should().Contain(blogId.ToString());
        staged.Content.Should().Contain("amazing-petra-trip");

        db.OutboxMessages.AsNoTracking().Any().Should().BeFalse();
    }
}
