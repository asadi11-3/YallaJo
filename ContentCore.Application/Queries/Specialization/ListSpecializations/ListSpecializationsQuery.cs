using ContentCore.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Specialization.ListSpecializations;

public sealed record SpecializationDto(
    Guid Id,
    string Name,
    string? Description,
    string? Icon,
    bool IsActive);

public sealed record ListSpecializationsQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<SpecializationDto>>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.Specializations(ActiveOnly);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
    public IReadOnlyList<string> Tags => ["specializations"];
}
