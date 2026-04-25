using Auth.Contracts.IntegrationEvents;
using ContentCore.Contracts.IntegrationEvents;
using ContentPlaces.Contracts.IntegrationEvents;
using ContentTours.Contracts;
using Security.Contracts.IntegrationEvents;

namespace YallaJo.SharedKernel.Infrastructure.Abstractions.Integration;

/// <summary>
/// Maps stable logical names to CLR types for integration events.
/// Every integration event MUST be registered here.
/// NEVER remove or rename an existing key — only add new ones.
/// To rename an event type, add a new v2 alias pointing to the same CLR type.
///
/// The stored OutboxMessage.Type column uses these short names, not
/// AssemblyQualifiedName — making refactors safe.
/// </summary>
public static class IntegrationEventTypeRegistry
{
    private static readonly Dictionary<string, Type> NameToType = new(StringComparer.Ordinal)
    {
        // ── Security (5 events) ──
        ["security.user.created.v1"]              = typeof(UserCreatedIntegrationEvent),
        ["security.user.email-verified.v1"]       = typeof(EmailVerifiedIntegrationEvent),
        ["security.user.password-changed.v1"]     = typeof(PasswordChangedIntegrationEvent),
        ["security.user.password-reset.v1"]       = typeof(PasswordResetIntegrationEvent),
        ["security.user.phone-updated.v1"]        = typeof(PhoneNumberUpdatedIntegrationEvent),

        // ── Auth (2 events) ──
        ["auth.user.logged-in.v1"]                = typeof(UserLoggedInIntegrationEvent),
        ["auth.session.revoked.v1"]               = typeof(SessionRevokedIntegrationEvent),

        // ── ContentCore (8 events) ──
        ["content-core.language.activated.v1"]         = typeof(LanguageActivatedIntegrationEvent),
        ["content-core.language.deactivated.v1"]       = typeof(LanguageDeactivatedIntegrationEvent),
        ["content-core.attachment.uploaded.v1"]        = typeof(AttachmentUploadedIntegrationEvent),
        ["content-core.attachment.deleted.v1"]         = typeof(AttachmentDeletedIntegrationEvent),
        ["content-core.category.created.v1"]           = typeof(CategoryCreatedIntegrationEvent),
        ["content-core.category.updated.v1"]           = typeof(CategoryUpdatedIntegrationEvent),
        ["content-core.category.deleted.v1"]           = typeof(CategoryDeletedIntegrationEvent),
        ["content-core.category.restored.v1"]          = typeof(CategoryRestoredIntegrationEvent),

        // ── ContentPlaces — Places (3 events) ──
        ["content-places.place.created.v1"]              = typeof(PlaceCreatedIntegrationEvent),
        ["content-places.place.updated.v1"]              = typeof(PlaceUpdatedIntegrationEvent),
        ["content-places.place.deleted.v1"]              = typeof(PlaceDeletedIntegrationEvent),

        // ── ContentPlaces — Businesses (5 events) ──
        ["content-places.business.created.v1"]           = typeof(BusinessCreatedIntegrationEvent),
        ["content-places.business.approved.v1"]          = typeof(BusinessApprovedIntegrationEvent),
        ["content-places.business.rejected.v1"]          = typeof(BusinessRejectedIntegrationEvent),
        ["content-places.business.suspended.v1"]         = typeof(BusinessSuspendedIntegrationEvent),
        ["content-places.business.reinstated.v1"]        = typeof(BusinessReinstatedIntegrationEvent),

        // ── ContentPlaces — ServiceItems (2 events) ──
        ["content-places.service-item.created.v1"]       = typeof(ServiceItemCreatedIntegrationEvent),
        ["content-places.service-item.deleted.v1"]       = typeof(ServiceItemDeletedIntegrationEvent),

        // ── ContentTours (1 event) ──
        ["content-tours.place.tour-count-updated.v1"]    = typeof(PlaceTourCountUpdatedIntegrationEvent),
    };

    private static readonly Dictionary<Type, string> TypeToName =
        NameToType
            .GroupBy(kv => kv.Value)
            .ToDictionary(g => g.Key, g => g.First().Key);

    /// <summary>Returns the stable short name for the given integration event type.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the type is not registered.</exception>
    public static string GetName(Type type)
        => TypeToName.TryGetValue(type, out var name)
            ? name
            : throw new InvalidOperationException(
                $"Integration event type '{type.FullName}' is not registered in " +
                $"{nameof(IntegrationEventTypeRegistry)}. Add it before publishing.");

    /// <summary>Tries to resolve the CLR type from a short registry name.</summary>
    public static bool TryGetType(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Type? type)
        => NameToType.TryGetValue(name, out type);

    /// <summary>All registered types — used by tests to verify completeness.</summary>
    public static IReadOnlyCollection<Type> AllRegisteredTypes => TypeToName.Keys;
}
