using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.Tour.SuggestTours;

public sealed class SuggestToursQueryHandler(
    ITourRepository tourRepo,
    ILogger<SuggestToursQueryHandler> logger)
    : IQueryHandler<SuggestToursQuery, IReadOnlyList<TourSuggestDto>>
{
    public async Task<Result<IReadOnlyList<TourSuggestDto>>> Handle(
        SuggestToursQuery query, CancellationToken ct)
    {
        var prefix = query.Q.Trim();

        // EF.Functions.Like generates LIKE 'prefix%' — index-friendly on CI collation.
        // Do NOT use t.Name.ToLower().StartsWith(...) — ToLower() prevents index seek.
        var dtos = await tourRepo
            .Query(asNoTracking: true)
            .Where(t => t.Status == TourStatus.Approved
                     && !t.IsDeleted
                     && EF.Functions.Like(t.Name, prefix + "%"))
            .OrderByDescending(t => t.BookingCount)
            .ThenBy(t => t.Name)
            .Take(10)
            .Select(t => new TourSuggestDto(t.Id, t.Name, t.Slug, null))
            .ToListAsync(ct);

        logger.LogDebug("SuggestTours for q='{Q}': {Count} results", query.Q, dtos.Count);

        return Result.Success((IReadOnlyList<TourSuggestDto>)dtos);
    }
}
