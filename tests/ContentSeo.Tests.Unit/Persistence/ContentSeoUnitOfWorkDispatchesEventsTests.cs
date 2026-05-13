using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Enums;
using ContentSeo.Domain.Events;
using ContentSeo.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Events;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentSeo.Tests.Unit.Persistence;

/// <summary>
/// PW-1 acceptance test (mirror of ContentBlogs equivalent).
///
/// Proves that <see cref="ContentSeoUnitOfWork"/> forwards <c>SaveChangesAsync</c>
/// to the SharedKernel <see cref="UnitOfWork{TContext}"/>, which collects domain events
/// from <see cref="YallaJo.SharedKernel.Domain.Entities.IAggregateRoot"/> instances in the
/// <see cref="ContentSeoDbContext"/> change tracker and dispatches them via
/// <see cref="IDomainEventDispatcher"/> BEFORE the underlying <c>SaveChangesAsync</c>.
///
/// Without the PW-1 fix the inner UoW was bypassed and no events were dispatched.
/// </summary>
public sealed class ContentSeoUnitOfWorkDispatchesEventsTests
{
    [Fact]
    public async Task SaveChangesAsync_WithAggregateRaisingEvent_DispatchesDomainEvent()
    {
        // Arrange ─────────────────────────────────────────────────────────────
        var options = new DbContextOptionsBuilder<ContentSeoDbContext>()
            .UseInMemoryDatabase(databaseName: $"contentseo-uow-{Guid.NewGuid()}")
            .Options;

        await using var context = new ContentSeoDbContext(options);

        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        dispatcher
            .DispatchAsync(Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var innerUow = new UnitOfWork<ContentSeoDbContext>(context, dispatcher);
        IContentSeoUnitOfWork sut = new ContentSeoUnitOfWork(innerUow);

        var seo = SeoMetadata.Create(
            entityType: SeoEntityType.Blog,
            entityId: Guid.CreateVersion7(),
            metaTitle: "test title");

        var domainEvent = new SeoMetadataCreatedDomainEvent(
            SeoMetadataId: seo.Id,
            EntityType: seo.EntityType,
            EntityId: seo.EntityId);

        // AddDomainEvent is public on BaseEntity — call directly.
        seo.AddDomainEvent(domainEvent);

        context.SeoMetadata.Add(seo);

        // Act ─────────────────────────────────────────────────────────────────
        var rowsAffected = await sut.SaveChangesAsync(CancellationToken.None);

        // Assert ──────────────────────────────────────────────────────────────
        rowsAffected.Should().BeGreaterThan(0,
            "the new SeoMetadata row should be persisted to the InMemory store");

        await dispatcher.Received(1).DispatchAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(events =>
                events != null && events.Any(e => e is SeoMetadataCreatedDomainEvent)),
            Arg.Any<CancellationToken>());

        seo.DomainEvents.Should().BeEmpty(
            "UnitOfWork<TContext> must clear domain events on each aggregate before SaveChanges");
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoAggregatesRaisingEvents_DoesNotInvokeDispatcher()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ContentSeoDbContext>()
            .UseInMemoryDatabase(databaseName: $"contentseo-uow-empty-{Guid.NewGuid()}")
            .Options;

        await using var context = new ContentSeoDbContext(options);
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        var innerUow = new UnitOfWork<ContentSeoDbContext>(context, dispatcher);
        IContentSeoUnitOfWork sut = new ContentSeoUnitOfWork(innerUow);

        var seo = SeoMetadata.Create(
            entityType: SeoEntityType.Blog,
            entityId: Guid.CreateVersion7());

        context.SeoMetadata.Add(seo);

        // Act
        _ = await sut.SaveChangesAsync(CancellationToken.None);

        // Assert
        await dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<IEnumerable<IDomainEvent>>(),
            Arg.Any<CancellationToken>());
    }
}
