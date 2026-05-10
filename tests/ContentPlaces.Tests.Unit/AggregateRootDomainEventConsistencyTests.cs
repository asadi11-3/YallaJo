using ContentPlaces.Domain.Entities;
using FluentAssertions;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentPlaces.Tests.Unit;

public sealed class AggregateRootDomainEventConsistencyTests
{
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
