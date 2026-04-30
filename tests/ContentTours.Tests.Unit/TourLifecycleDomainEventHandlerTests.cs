using ContentTours.Domain.Events;
using ContentTours.Infrastructure.EventHandlers;
using ContentTours.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Tests.Unit;

/// <summary>
/// Phase-4 Infrastructure domain-event handlers (outbox-only, no SaveChanges).
/// Each test instantiates the real handler against an EF Core in-memory
/// <see cref="ContentToursDbContext"/>, dispatches the corresponding domain event,
/// and asserts a single staged <see cref="OutboxMessage"/> with the correct
/// stable type name from <see cref="YallaJo.SharedKernel.Infrastructure.Abstractions.Integration.IntegrationEventTypeRegistry"/>.
/// </summary>
public sealed class TourLifecycleDomainEventHandlerTests
{
    private static ContentToursDbContext NewInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ContentToursDbContext>()
            .UseInMemoryDatabase(databaseName: $"tours-lifecycle-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ContentToursDbContext(options);
    }

    private static OutboxMessage SingleStaged(ContentToursDbContext db) =>
        db.ChangeTracker
          .Entries<OutboxMessage>()
          .Where(e => e.State == EntityState.Added)
          .Select(e => e.Entity)
          .Should().ContainSingle().Subject;

    [Fact]
    public async Task TourSubmitted_StagesOneOutboxRowWithSubmittedTypeName()
    {
        await using var db = NewInMemoryDbContext();
        var handler = new TourSubmittedDomainEventHandler(
            db, Substitute.For<ILogger<TourSubmittedDomainEventHandler>>());

        var evt = new TourSubmittedDomainEvent(
            TourId: Guid.NewGuid(),
            CreatedByUserId: Guid.NewGuid(),
            SubmittedAt: DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<TourSubmittedDomainEvent>(evt),
            CancellationToken.None);

        SingleStaged(db).Type.Should().Be("content-tours.tour.submitted.v1");
    }

    [Fact]
    public async Task TourApproved_StagesOneOutboxRowWithApprovedTypeName()
    {
        await using var db = NewInMemoryDbContext();
        var handler = new TourApprovedDomainEventHandler(
            db, Substitute.For<ILogger<TourApprovedDomainEventHandler>>());

        var evt = new TourApprovedDomainEvent(
            TourId: Guid.NewGuid(),
            CreatedByUserId: Guid.NewGuid(),
            ApprovedByUserId: Guid.NewGuid(),
            ApprovedAt: DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<TourApprovedDomainEvent>(evt),
            CancellationToken.None);

        SingleStaged(db).Type.Should().Be("content-tours.tour.approved.v1");
    }

    [Fact]
    public async Task TourRejected_StagesOneOutboxRowWithRejectedTypeNameAndCarriesReason()
    {
        // Privacy contract is enforced via XML-doc + structured-log convention; no
        // logging assertion is made here because the project lacks a shared
        // ILogger-capture helper. This test pins the outbox payload contract: Reason
        // MUST be present in the integration event for downstream notification, but
        // the handler MUST NOT log it at Information level (verified by code review).
        await using var db = NewInMemoryDbContext();
        var handler = new TourRejectedDomainEventHandler(
            db, Substitute.For<ILogger<TourRejectedDomainEventHandler>>());

        var evt = new TourRejectedDomainEvent(
            TourId: Guid.NewGuid(),
            CreatedByUserId: Guid.NewGuid(),
            RejectedByUserId: Guid.NewGuid(),
            Reason: "Photos too blurry.",
            RejectedAt: DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<TourRejectedDomainEvent>(evt),
            CancellationToken.None);

        var staged = SingleStaged(db);
        staged.Type.Should().Be("content-tours.tour.rejected.v1");
        staged.Content.Should().Contain("\"Photos too blurry.\"");
    }

    [Fact]
    public async Task TourSuspended_StagesOneOutboxRowWithSuspendedTypeNameAndCarriesReason()
    {
        await using var db = NewInMemoryDbContext();
        var handler = new TourSuspendedDomainEventHandler(
            db, Substitute.For<ILogger<TourSuspendedDomainEventHandler>>());

        var evt = new TourSuspendedDomainEvent(
            TourId: Guid.NewGuid(),
            CreatedByUserId: Guid.NewGuid(),
            Reason: "Compliance hold.",
            SuspendedAt: DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<TourSuspendedDomainEvent>(evt),
            CancellationToken.None);

        var staged = SingleStaged(db);
        staged.Type.Should().Be("content-tours.tour.suspended.v1");
        staged.Content.Should().Contain("\"Compliance hold.\"");
    }

    [Fact]
    public async Task TourReinstated_StagesOneOutboxRowWithReinstatedTypeName()
    {
        await using var db = NewInMemoryDbContext();
        var handler = new TourReinstatedDomainEventHandler(
            db, Substitute.For<ILogger<TourReinstatedDomainEventHandler>>());

        var evt = new TourReinstatedDomainEvent(
            TourId: Guid.NewGuid(),
            CreatedByUserId: Guid.NewGuid(),
            ReinstatedAt: DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<TourReinstatedDomainEvent>(evt),
            CancellationToken.None);

        SingleStaged(db).Type.Should().Be("content-tours.tour.reinstated.v1");
    }
}
