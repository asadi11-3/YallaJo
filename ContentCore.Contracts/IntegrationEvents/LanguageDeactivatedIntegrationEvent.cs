using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Contracts.IntegrationEvents;

/// <summary>
/// Published when a language is deactivated in ContentCore.
/// Downstream modules (ContentPlaces, ContentTours, ContentSeo, ContentBlogs) can consume
/// this event to stop auto-translating into the deactivated language.
///
/// <para><b>Note — no consumers registered yet.</b> Event queues in each module's outbox
/// and will be consumed by future handlers without registry changes.
/// See <c>Agents/ContentCore-gap-fix-plan.md</c> GAP-05 for details.</para>
/// </summary>
public sealed record LanguageDeactivatedIntegrationEvent(
    Guid LanguageId,
    string LanguageCode) : IntegrationEventBase;
