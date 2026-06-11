using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.Place.SuggestPlaces;

/// <summary>
/// Returns up to 10 non-deleted places whose name starts with the requested prefix.
/// Default-language names only (translations intentionally skipped — suggestions
/// feed admin lookups and public typeahead where canonical names are expected).
/// </summary>
public sealed class SuggestPlacesQueryHandler(
    IPlaceRepository placeRepository,
    ILogger<SuggestPlacesQueryHandler> logger) : IQueryHandler<SuggestPlacesQuery, IReadOnlyList<PlaceSuggestDto>>
{
    private const int MaxSuggestions = 10;

    public async Task<Result<IReadOnlyList<PlaceSuggestDto>>> Handle(SuggestPlacesQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var pattern = query.Q.Trim() + "%";

            var dtos = await placeRepository
                .Query(asNoTracking: true)
                .Where(p => !p.IsDeleted && EF.Functions.Like(p.Name, pattern))
                .OrderBy(p => p.Name)
                .Take(MaxSuggestions)
                .Select(p => new PlaceSuggestDto(p.Id, p.Name, p.Slug))
                .ToListAsync(cancellationToken);

            logger.LogDebug("Place suggest for '{Prefix}' returned {Count} results.", query.Q, dtos.Count);

            return Result.Success((IReadOnlyList<PlaceSuggestDto>)dtos);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<IReadOnlyList<PlaceSuggestDto>>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
