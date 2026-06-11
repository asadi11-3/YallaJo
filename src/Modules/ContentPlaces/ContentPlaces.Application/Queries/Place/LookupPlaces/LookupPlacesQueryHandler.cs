using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.Place.LookupPlaces;

/// <summary>
/// [Backend] B2 — single indexed query over Place.Name; excludes soft-deleted places.
/// Empty term returns the first N places alphabetically (mirrors the legacy first-page select).
/// </summary>
public sealed class LookupPlacesQueryHandler(
    IPlaceRepository placeRepository,
    ILogger<LookupPlacesQueryHandler> logger)
    : IQueryHandler<LookupPlacesQuery, IReadOnlyList<PlaceLookupDto>>
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 20;

    public async Task<Result<IReadOnlyList<PlaceLookupDto>>> Handle(
        LookupPlacesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var pageSize = Math.Clamp(request.PageSize, MinPageSize, MaxPageSize);
            var term = request.Term?.Trim();

            var q = placeRepository.Query(asNoTracking: true)
                .Where(p => !p.IsDeleted);

            if (!string.IsNullOrEmpty(term))
            {
                // Contains (translated to LIKE '%term%') — case-insensitivity follows DB collation.
                q = q.Where(p => p.Name.Contains(term));
            }

            var items = await q
                .OrderBy(p => p.Name)
                .Take(pageSize)
                .Select(p => new PlaceLookupDto(p.Id, p.Name, p.City))
                .ToListAsync(cancellationToken);

            logger.LogDebug("Place lookup term='{Term}' returned {Count} rows", term, items.Count);

            return Result.Success<IReadOnlyList<PlaceLookupDto>>(items);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyList<PlaceLookupDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
