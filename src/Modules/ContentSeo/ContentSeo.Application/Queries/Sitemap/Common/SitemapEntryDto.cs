using ContentSeo.Domain.Entities;

namespace ContentSeo.Application.Queries.Sitemap.Common;

public sealed record SitemapEntryDto(
    Guid Id,
    string Url,
    string EntityType,
    Guid? EntityId,
    decimal? Priority,
    string? ChangeFrequency,
    DateTime? LastModified,
    bool IsActive)
{
    public static SitemapEntryDto From(SitemapEntry entry) => new(
        entry.Id,
        entry.Url,
        entry.EntityType,
        entry.EntityId,
        entry.Priority,
        entry.ChangeFrequency,
        entry.LastModified,
        entry.IsActive);
}
