using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Contracts.IntegrationEvents;

/// <summary>
/// Published when a file is uploaded and linked to an entity in ContentCore.
/// Downstream modules (e.g. ContentSeo) can consume this to auto-populate
/// OgImageUrl / media metadata when the attachment is an image.
///
/// <para><b>Note — no consumers registered yet.</b> See Agents/ContentCore-deferred-gaps-plan.md §3.</para>
/// </summary>
public sealed record AttachmentUploadedIntegrationEvent(
    Guid AttachmentId,
    string EntityType,
    Guid EntityId,
    string AttachmentType,
    string Url,
    bool IsPrimary) : IntegrationEventBase;
