using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Contracts.IntegrationEvents;

/// <summary>
/// Published when a category is created in ContentCore.
/// Downstream modules (e.g. ContentSeo) can consume this to auto-create SeoMetadata.
///
/// <para>Payload intentionally excludes translations — consumers query ContentCore if needed.</para>
/// </summary>
public sealed record CategoryCreatedIntegrationEvent(
    Guid CategoryId,
    string Name,
    string Slug,
    Guid? ParentCategoryId,
    string SourceLanguageCode) : IntegrationEventBase;
