using System.Text.Json;
using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.EventHandlers;
using ContentTours.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Tests.Unit;

/// <summary>
/// Phase-0 deferred sanity test for the create-to-outbox path.
/// Drives the actual <see cref="TourCreatedDomainEventHandler"/> against an EF Core
/// in-memory <see cref="ContentToursDbContext"/> and asserts:
///   1. After dispatch, exactly one OutboxMessage is staged on the DbContext.
///   2. The staged Type matches the registry entry <c>content-tours.tour.created.v1</c>.
///   3. The Content JSON deserialises back to <see cref="TourCreatedIntegrationEvent"/>.
///   4. When the translation orchestrator throws, the outbox row is still staged
///      (translation must NEVER fail the originating command).
/// </summary>
public sealed class TourCreatedDomainEventHandlerTests
{
    private static ContentToursDbContext NewInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ContentToursDbContext>()
            .UseInMemoryDatabase(databaseName: $"tours-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ContentToursDbContext(options);
    }

    private static TourCreatedDomainEvent NewEvent() => new(
        TourId:          Guid.NewGuid(),
        Name:            "Petra Day Tour",
        Description:     "A long descriptive paragraph about the tour itinerary.",
        ShortDescription:"Walk through the rose-red city.",
        MetaTitle:       null,
        MetaDescription: null,
        Slug:            "petra-day-tour",
        CreatedByUserId: Guid.NewGuid(),
        PlaceId:         null);

    [Fact]
    public async Task StagesExactlyOneTourCreatedIntegrationEventOnTheOutbox()
    {
        await using var db = NewInMemoryDbContext();
        var orchestrator = Substitute.For<IEntityTranslationOrchestrator>();
        orchestrator
            .TranslateToAllActiveLanguagesAsync(
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EntityFieldTranslationSet>());

        var logger = Substitute.For<ILogger<TourCreatedDomainEventHandler>>();
        var handler = new TourCreatedDomainEventHandler(orchestrator, db, logger);

        var evt = NewEvent();
        await handler.Handle(new DomainEventNotification<TourCreatedDomainEvent>(evt), CancellationToken.None);

        // Outbox row was staged on the DbContext (Added) — UoW will commit it atomically.
        var addedOutbox = db.ChangeTracker
            .Entries<OutboxMessage>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        addedOutbox.Should().HaveCount(1);
        addedOutbox[0].Type.Should().Be("content-tours.tour.created.v1");

        var deserialised = JsonSerializer.Deserialize<TourCreatedIntegrationEvent>(addedOutbox[0].Content)!;
        deserialised.TourId.Should().Be(evt.TourId);
        deserialised.Slug.Should().Be(evt.Slug);
        deserialised.CreatedByUserId.Should().Be(evt.CreatedByUserId);
    }

    [Fact]
    public async Task TranslationFailureDoesNotPreventOutboxStaging()
    {
        await using var db = NewInMemoryDbContext();
        var orchestrator = Substitute.For<IEntityTranslationOrchestrator>();
        orchestrator
            .TranslateToAllActiveLanguagesAsync(
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Azure Translator unavailable."));

        var logger = Substitute.For<ILogger<TourCreatedDomainEventHandler>>();
        var handler = new TourCreatedDomainEventHandler(orchestrator, db, logger);

        var evt = NewEvent();

        // Must NOT throw — translation is best-effort.
        await handler.Handle(new DomainEventNotification<TourCreatedDomainEvent>(evt), CancellationToken.None);

        // Outbox row was still staged before the translation attempt.
        db.ChangeTracker.Entries<OutboxMessage>()
          .Count(e => e.State == EntityState.Added)
          .Should().Be(1);
    }
}
