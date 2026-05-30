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
        // F7 fix (Findings-Rolling.md): empty trending window means warming-up, not server error.
        // Return 200 OK with empty list rather than 500. Clients treat empty list as "no trending yet".
        return Result.Success((IReadOnlyList<PopularEntityDto>)all.OrderBy(x => x.TrendingRank).Take(request.Count).ToList());
    }
}
