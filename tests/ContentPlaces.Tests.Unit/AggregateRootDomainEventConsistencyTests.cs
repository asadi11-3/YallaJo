using ContentPlaces.Domain.Entities;
using FluentAssertions;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentPlaces.Tests.Unit;

/// <summary>
/// CONTENTPLACES-FOLLOWUP-AGGREGATE-ROOT-001 architectural consistency test.
///
/// <para>
/// The shared <c>UnitOfWork&lt;TContext&gt;.SaveChangesAsync</c> only
/// dispatches domain events from entities tracked as
/// <c>IAggregateRoot</c>.  Any entity that intends to raise domain events
/// MUST therefore implement <see cref="IAggregateRoot"/>; otherwise events
/// would be silently dropped — and reversing the omission later (without
/// also removing any compensating manual outbox enqueue) would cause
/// <b>double-publish</b> of integration events.
/// </para>
///
/// <para>
/// This test pins the canonical aggregate-root membership matrix for the
/// ContentPlaces domain so the foot-gun cannot be silently reintroduced.
/// Aggregate roots: <c>Place</c>, <c>Business</c>.  Non-aggregate child
/// entities (which use <c>IContentPlacesOutboxWriter</c> when integration
/// events are required): everything else.
/// </para>
///
/// <para>
/// If a future change adds <see cref="IAggregateRoot"/> to one of the
/// non-aggregate entries below, this test fails — forcing the contributor
/// to also remove any manual outbox enqueue from the corresponding command
/// handlers (preventing double-publish).  Conversely, if a future change
/// removes <see cref="IAggregateRoot"/> from <c>Place</c> or
/// <c>Business</c>, this test fails — forcing the contributor to ensure no
/// domain events are dropped.
/// </para>
/// </summary>
public sealed class AggregateRootDomainEventConsistencyTests
{
    // ── Aggregate roots — must implement IAggregateRoot ──────────────────────

    [Fact]
    public void Place_IsAggregateRoot()
    {
        typeof(Place).Should().BeAssignableTo<IAggregateRoot>(
            "Place is the canonical Place aggregate root and raises domain events " +
            "(PlaceCreatedDomainEvent, PlaceUpdatedDomainEvent, PlaceDeletedDomainEvent).");
    }

    [Fact]
    public void Business_IsAggregateRoot()
    {
        typeof(Business).Should().BeAssignableTo<IAggregateRoot>(
            "Business is the canonical Business aggregate root and raises domain events " +
            "(BusinessCreatedDomainEvent, BusinessUpdatedDomainEvent, lifecycle events).");
    }

    // ── Non-aggregate child entities — must NOT implement IAggregateRoot ─────

    [Theory]
    [MemberData(nameof(NonAggregateChildEntityTypes))]
    public void NonAggregateChildEntity_DoesNotImplementIAggregateRoot(Type entityType)
    {
        entityType.Should().NotBeAssignableTo<IAggregateRoot>(
            $"{entityType.Name} is a non-aggregate child entity in the ContentPlaces " +
            "domain. Promoting it to IAggregateRoot would activate the shared " +
            "UnitOfWork's domain-event dispatcher for this entity — and if any " +
            "command handler also enqueues an integration event manually via " +
            "IContentPlacesOutboxWriter, that would cause double-publish " +
            "(CONTENTPLACES-FOLLOWUP-AGGREGATE-ROOT-001).");
    }

    /// <summary>
    /// Manually-maintained whitelist of every non-aggregate entity in
    /// <c>ContentPlaces.Domain.Entities</c>.  Update this list (and the
    /// reasoning for the change) when adding or removing entities.
    /// </summary>
    public static IEnumerable<object[]> NonAggregateChildEntityTypes()
    {
        yield return new object[] { typeof(BusinessStaff) };
        yield return new object[] { typeof(ServiceItem) };
        yield return new object[] { typeof(BusinessHours) };
        yield return new object[] { typeof(BusinessAmenity) };
        yield return new object[] { typeof(BusinessTranslation) };
        yield return new object[] { typeof(PlaceTranslation) };
        yield return new object[] { typeof(PlaceBusiness) };
        yield return new object[] { typeof(AccessibilityFeature) };
    }
}
