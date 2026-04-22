using Auth.Contracts.IntegrationEvents;
using ContentCore.Contracts.IntegrationEvents;
using ContentPlaces.Contracts.IntegrationEvents;
using FluentAssertions;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Infrastructure.Abstractions.Integration;

namespace SharedKernel.Tests.Unit;

/// <summary>
/// Verifies IntegrationEventTypeRegistry correctness and completeness.
/// </summary>
public sealed class IntegrationEventTypeRegistryTests
{
    // ── All 13 expected types ────────────────────────────────────────────────

    private static readonly (string Key, Type Type)[] KnownMappings =
    [
        ("security.user.created.v1",               typeof(UserCreatedIntegrationEvent)),
        ("security.user.email-verified.v1",        typeof(EmailVerifiedIntegrationEvent)),
        ("security.user.password-changed.v1",      typeof(PasswordChangedIntegrationEvent)),
        ("security.user.password-reset.v1",        typeof(PasswordResetIntegrationEvent)),
        ("security.user.phone-updated.v1",         typeof(PhoneNumberUpdatedIntegrationEvent)),
        ("auth.user.logged-in.v1",                 typeof(UserLoggedInIntegrationEvent)),
        ("auth.session.revoked.v1",                typeof(SessionRevokedIntegrationEvent)),
        ("content-core.language.activated.v1",     typeof(LanguageActivatedIntegrationEvent)),
        ("content-places.place.created.v1",        typeof(PlaceCreatedIntegrationEvent)),
        ("content-places.place.updated.v1",        typeof(PlaceUpdatedIntegrationEvent)),
        ("content-places.place.deleted.v1",        typeof(PlaceDeletedIntegrationEvent)),
        ("content-places.service-item.created.v1", typeof(ServiceItemCreateIntegrationEvent)),
        ("content-places.service-item.deleted.v1", typeof(ServiceItemDeletedIntegrationEvent)),
    ];

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void AllRegisteredTypes_Contains_All13Events()
    {
        var registered = IntegrationEventTypeRegistry.AllRegisteredTypes;

        registered.Should().HaveCount(13, "all 13 integration events must be registered");

        foreach (var (_, type) in KnownMappings)
        {
            registered.Should().Contain(type, $"{type.Name} must be in AllRegisteredTypes");
        }
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
