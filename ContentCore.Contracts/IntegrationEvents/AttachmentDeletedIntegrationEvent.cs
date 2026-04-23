using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Contracts.IntegrationEvents;

/// <summary>
/// Published when an attachment is deleted from ContentCore.
/// Downstream modules (e.g. ContentSeo) can consume this to clear cached
/// OgImageUrl when the primary image for a Place/Business/Tour is removed.
///
/// <para><b>Note — no consumers registered yet.</b> See Agents/ContentCore-deferred-gaps-plan.md §3.</para>
/// </summary>
public sealed record AttachmentDeletedIntegrationEvent(
    Guid AttachmentId,
    string EntityType,
    Guid EntityId,
    string AttachmentType,
    string Url) : IntegrationEventBase;
