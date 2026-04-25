using ContentCore.Application.Caching;
using ContentCore.Application.Queries.Specialization.ListSpecializations;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Specialization.GetSpecializationById;

public sealed record GetSpecializationByIdQuery(Guid Id)
    : IQuery<SpecializationDto>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.SpecializationById(Id);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
    public IReadOnlyList<string> Tags => ["specializations", $"specialization:{Id}"];
}
