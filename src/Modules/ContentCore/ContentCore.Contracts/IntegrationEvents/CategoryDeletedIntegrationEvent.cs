using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Contracts.IntegrationEvents;

/// <summary>
/// Published when a category is soft-deleted in ContentCore.
/// Downstream modules can consume this to orphan or remove linked SeoMetadata / content.
/// </summary>
public sealed record CategoryDeletedIntegrationEvent(
    Guid CategoryId,
    string Slug) : IntegrationEventBase;
