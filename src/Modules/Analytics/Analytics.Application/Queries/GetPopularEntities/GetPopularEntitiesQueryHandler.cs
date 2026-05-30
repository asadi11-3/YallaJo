using Analytics.Application.Interfaces.Repositories;
using Analytics.Application.Models;
using Analytics.Domain.Enums;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetPopularEntities;

public sealed class GetPopularEntitiesQueryHandler(IPopularityScoreRepository repo, ILogger<GetPopularEntitiesQueryHandler> logger) : IQueryHandler<GetPopularEntitiesQuery, IReadOnlyList<PopularEntityDto>>
{
    public async Task<Result<IReadOnlyList<PopularEntityDto>>> Handle(GetPopularEntitiesQuery request, CancellationToken ct)
    {
        if (!Enum.TryParse<EntityType>(request.EntityType, true, out var type)) return Result.Failure<IReadOnlyList<PopularEntityDto>>(new Error("Analytics.InvalidEntityType", "Unsupported entity type."));
        var rows = await repo.GetTopByTypeAsync(type, request.Count, ct);
        logger.LogDebug("Read {Count} popular {Type}", rows.Count, type);
        return Result.Success((IReadOnlyList<PopularEntityDto>)rows.Select(x => new PopularEntityDto(x.EntityId, x.EntityType.ToString(), x.Score, x.TrendingRank, 0)).ToList());
    }
}
