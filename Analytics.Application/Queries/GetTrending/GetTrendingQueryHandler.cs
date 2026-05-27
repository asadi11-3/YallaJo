using Analytics.Application.Interfaces.Repositories;
using Analytics.Application.Models;
using Analytics.Domain.Enums;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetTrending;

public sealed class GetTrendingQueryHandler(IPopularityScoreRepository repo, ILogger<GetTrendingQueryHandler> logger) : IQueryHandler<GetTrendingQuery, IReadOnlyList<PopularEntityDto>>
{
    public async Task<Result<IReadOnlyList<PopularEntityDto>>> Handle(GetTrendingQuery request, CancellationToken ct)
    {
        var all = new List<PopularEntityDto>();
        foreach (var type in Enum.GetValues<EntityType>()) all.AddRange((await repo.GetTrendingAsync(type, request.Count, ct)).Select(x => new PopularEntityDto(x.EntityId, x.EntityType.ToString(), x.Score, x.TrendingRank, 0)));
        logger.LogDebug("Read {Count} trending entities", all.Count);
        return all.Count == 0 ? Result.Failure<IReadOnlyList<PopularEntityDto>>(new Error("Trending.WindowNotReady", "Trending window is not ready."), Outcome.ServerError) : Result.Success((IReadOnlyList<PopularEntityDto>)all.OrderBy(x => x.TrendingRank).Take(request.Count).ToList());
    }
}
