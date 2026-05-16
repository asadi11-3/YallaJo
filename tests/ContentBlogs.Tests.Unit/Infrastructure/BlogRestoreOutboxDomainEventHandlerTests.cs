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
/// Outbox-only domain-event handler for Blog restore.
/// Stages exactly one <see cref="OutboxMessage"/> with the registered
/// logical name <c>content-blogs.blog.restored.v1</c> and never calls
/// <c>SaveChangesAsync</c>.
/// </summary>
public sealed class BlogRestoreOutboxDomainEventHandlerTests
{
    private static ContentBlogsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-restore-outbox-{Guid.NewGuid():N}")
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
    public async Task BlogRestoredDomainEventHandler_WritesBlogRestoredOutboxMessage()
    {
        await using var db = NewDb();
        var handler = new BlogRestoredDomainEventHandler(
            db, Substitute.For<ILogger<BlogRestoredDomainEventHandler>>());

        var blogId = Guid.NewGuid();
        var evt = new BlogRestoredDomainEvent(
            BlogId:        blogId,
            Slug:          "amazing-petra-trip",
            RestoredAtUtc: DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<BlogRestoredDomainEvent>(evt),
            CancellationToken.None);

        var staged = SingleStaged(db);
        staged.Type.Should().Be("content-blogs.blog.restored.v1");
        staged.Content.Should().Contain(blogId.ToString());
        staged.Content.Should().Contain("amazing-petra-trip");

        // Handler MUST NOT call SaveChangesAsync — the row is staged in
        // ChangeTracker but not yet persisted.
        db.OutboxMessages.AsNoTracking().Any().Should().BeFalse();
    }
}
