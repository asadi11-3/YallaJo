using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Contracts.IntegrationEvents;

/// <summary>
/// Published when a soft-deleted category is restored in ContentCore.
/// Downstream modules can consume this to re-link orphaned SeoMetadata / content.
/// </summary>
public sealed record CategoryRestoredIntegrationEvent(
    Guid CategoryId,
    string Slug) : IntegrationEventBase;
