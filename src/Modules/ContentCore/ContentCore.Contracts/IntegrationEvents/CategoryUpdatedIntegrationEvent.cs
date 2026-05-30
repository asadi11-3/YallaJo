using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Contracts.IntegrationEvents;

/// <summary>
/// Published when a category's name or slug is updated in ContentCore.
/// Downstream modules (e.g. ContentSeo) can consume this to invalidate cached slugs.
/// </summary>
public sealed record CategoryUpdatedIntegrationEvent(
    Guid CategoryId,
    string Name,
    string Slug,
    string SourceLanguageCode) : IntegrationEventBase;
