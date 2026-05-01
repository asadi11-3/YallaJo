using Auth.Contracts.IntegrationEvents;
using ContentCore.Contracts.IntegrationEvents;
using ContentPlaces.Contracts.IntegrationEvents;
using ContentTours.Contracts.IntegrationEvents;
using FluentAssertions;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Infrastructure.Abstractions.Integration;

namespace SharedKernel.Tests.Unit;

/// <summary>
/// Verifies <see cref="IntegrationEventTypeRegistry"/> correctness and completeness.
///
/// The expected count is derived from <see cref="KnownMappings"/> (not hardcoded) so
/// adding a new integration event in any module simply means appending one row here —
/// no scattered count update is required. Two completeness assertions run together:
///
///   1. Every <see cref="KnownMappings"/> entry must be present in the registry.
///   2. Every type in <see cref="IntegrationEventTypeRegistry.AllRegisteredTypes"/>
///      must also be in <see cref="KnownMappings"/>. This catches the reverse drift —
///      a production event added to the registry without a corresponding test row.
/// </summary>
public sealed class IntegrationEventTypeRegistryTests
{
    // ── All registered integration events ─────────────────────────────────────
    //
    // When you add a new integration event:
    //   1) register it in IntegrationEventTypeRegistry.cs (production)
    //   2) append one row here
    // No count constants need updating — both assertions below derive from this list.

    private static readonly (string Key, Type Type)[] KnownMappings =
    [
        // Security (5)
        ("security.user.created.v1",                      typeof(UserCreatedIntegrationEvent)),
        ("security.user.email-verified.v1",               typeof(EmailVerifiedIntegrationEvent)),
        ("security.user.password-changed.v1",             typeof(PasswordChangedIntegrationEvent)),
        ("security.user.password-reset.v1",               typeof(PasswordResetIntegrationEvent)),
        ("security.user.phone-updated.v1",                typeof(PhoneNumberUpdatedIntegrationEvent)),

        // Auth (2)
        ("auth.user.logged-in.v1",                        typeof(UserLoggedInIntegrationEvent)),
        ("auth.session.revoked.v1",                       typeof(SessionRevokedIntegrationEvent)),

        // ContentCore (8)
        ("content-core.language.activated.v1",            typeof(LanguageActivatedIntegrationEvent)),
        ("content-core.language.deactivated.v1",          typeof(LanguageDeactivatedIntegrationEvent)),
        ("content-core.attachment.uploaded.v1",           typeof(AttachmentUploadedIntegrationEvent)),
        ("content-core.attachment.deleted.v1",            typeof(AttachmentDeletedIntegrationEvent)),
        ("content-core.category.created.v1",              typeof(CategoryCreatedIntegrationEvent)),
        ("content-core.category.updated.v1",              typeof(CategoryUpdatedIntegrationEvent)),
        ("content-core.category.deleted.v1",              typeof(CategoryDeletedIntegrationEvent)),
        ("content-core.category.restored.v1",             typeof(CategoryRestoredIntegrationEvent)),

        // ContentPlaces — Places (3)
        ("content-places.place.created.v1",               typeof(PlaceCreatedIntegrationEvent)),
        ("content-places.place.updated.v1",               typeof(PlaceUpdatedIntegrationEvent)),
        ("content-places.place.deleted.v1",               typeof(PlaceDeletedIntegrationEvent)),

        // ContentPlaces — Businesses (5)
        ("content-places.business.created.v1",            typeof(BusinessCreatedIntegrationEvent)),
        ("content-places.business.approved.v1",           typeof(BusinessApprovedIntegrationEvent)),
        ("content-places.business.rejected.v1",           typeof(BusinessRejectedIntegrationEvent)),
        ("content-places.business.suspended.v1",          typeof(BusinessSuspendedIntegrationEvent)),
        ("content-places.business.reinstated.v1",         typeof(BusinessReinstatedIntegrationEvent)),

        // ContentPlaces — ServiceItems (2)
        ("content-places.service-item.created.v1",        typeof(ServiceItemCreatedIntegrationEvent)),
        ("content-places.service-item.deleted.v1",        typeof(ServiceItemDeletedIntegrationEvent)),

        // ContentTours — pre-Task-1 (4)
        ("content-tours.place.tour-count-updated.v1",     typeof(PlaceTourCountUpdatedIntegrationEvent)),
        ("content-tours.schedule.changed.v1",             typeof(TourScheduleChangedIntegrationEvent)),
        ("content-tours.pricing-tier.changed.v1",         typeof(TourPricingTierChangedIntegrationEvent)),
        ("content-tours.tour.featured-changed.v1",        typeof(TourFeaturedChangedIntegrationEvent)),

        // ContentTours — Task-1 lifecycle (8)
        ("content-tours.tour.deleted.v1",                 typeof(TourDeletedIntegrationEvent)),
        ("content-tours.tour.created.v1",                 typeof(TourCreatedIntegrationEvent)),
        ("content-tours.tour.updated.v1",                 typeof(TourUpdatedIntegrationEvent)),
        ("content-tours.tour.submitted.v1",               typeof(TourSubmittedIntegrationEvent)),
        ("content-tours.tour.approved.v1",                typeof(TourApprovedIntegrationEvent)),
        ("content-tours.tour.rejected.v1",                typeof(TourRejectedIntegrationEvent)),
        ("content-tours.tour.suspended.v1",               typeof(TourSuspendedIntegrationEvent)),
        ("content-tours.tour.reinstated.v1",              typeof(TourReinstatedIntegrationEvent)),
    ];

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void AllRegisteredTypes_ContainsEveryKnownMapping()
    {
        // Forward direction: every (key, type) the test knows about must be registered.
        var registered = IntegrationEventTypeRegistry.AllRegisteredTypes;

        foreach (var (_, type) in KnownMappings)
        {
            registered.Should().Contain(type, $"{type.Name} must be in AllRegisteredTypes");
        }
    }

    [Fact]
    public void AllRegisteredTypes_HasNoUnknownEntriesBeyondKnownMappings()
    {
        // Reverse direction: every registered type must also appear in KnownMappings.
        // If a developer adds a new event to the production registry, this assertion fails
        // until the corresponding test row is appended above.
        var registered = IntegrationEventTypeRegistry.AllRegisteredTypes;
        var known = KnownMappings.Select(m => m.Type).ToHashSet();

        registered.Should().OnlyContain(
            t => known.Contains(t),
            "every registered integration event must have a matching row in KnownMappings " +
            "(append the new (key, type) pair when you register a new event in production)");
    }

    [Fact]
    public void AllRegisteredTypes_CountMatchesKnownMappings()
    {
        // Count is derived (not hardcoded) so adding events does not require touching
        // a magic number. Together with the two checks above this pins exact parity.
        var registered = IntegrationEventTypeRegistry.AllRegisteredTypes;

        registered.Should().HaveCount(
            KnownMappings.Length,
            "registry size must equal the number of KnownMappings rows");
    }

    [Theory]
    [MemberData(nameof(GetKnownMappings))]
    public void GetName_ReturnsCorrectShortKey(string expectedKey, Type type)
    {
        var name = IntegrationEventTypeRegistry.GetName(type);
        name.Should().Be(expectedKey);
    }

    [Theory]
    [MemberData(nameof(GetKnownMappings))]
    public void TryGetType_ReturnsCorrectType(string key, Type expectedType)
    {
        var found = IntegrationEventTypeRegistry.TryGetType(key, out var type);

        found.Should().BeTrue();
        type.Should().Be(expectedType);
    }

    [Fact]
    public void GetName_UnregisteredType_Throws()
    {
        var act = () => IntegrationEventTypeRegistry.GetName(typeof(object));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not registered*");
    }

    [Fact]
    public void TryGetType_UnknownKey_ReturnsFalse()
    {
        var found = IntegrationEventTypeRegistry.TryGetType("nonexistent.event.v1", out var type);

        found.Should().BeFalse();
        type.Should().BeNull();
    }

    // ── MemberData helper ────────────────────────────────────────────────────

    public static IEnumerable<object[]> GetKnownMappings()
        => KnownMappings.Select(m => new object[] { m.Key, m.Type });
}
