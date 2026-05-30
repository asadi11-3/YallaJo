using Analytics.Application.Models;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetProviderMyTours;

public sealed record GetProviderMyToursQuery(Guid ProviderId, long? AfterId, int PageSize) : IQuery<CursorPageDto<ProviderTourListItemDto>>, ICacheableQuery
{
    public string CacheKey => $"provider:tours:{ProviderId}:{AfterId}:{PageSize}";
    public TimeSpan? CacheDuration => TimeSpan.FromSeconds(60);
    public IReadOnlyList<string> Tags => [$"provider:tours:{ProviderId}"];
}
