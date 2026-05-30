using ContentTours.Application.Queries.Tour.Common;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.Tour.SearchTours;

public sealed class SearchToursQueryHandler(
    ITourRepository tourRepo,
    ILogger<SearchToursQueryHandler> logger)
    : IQueryHandler<SearchToursQuery, SearchToursResult>
{
    public async Task<Result<SearchToursResult>> Handle(
        SearchToursQuery query, CancellationToken ct)
    {
        try
        {
        var req = query.Request;

        // 1. Tokenize
        var tokens = SearchTokenizer.Tokenize(req.Q);

        // Guard: no tokens AND no filters → reject
        if (tokens.Count == 0 && !HasAnyFilter(req))
            return Result.Invalid<SearchToursResult>(
                new Error("Tour.SearchQueryRequired",
                    "Provide a search query or at least one filter."));

        // 2. Build filtered IQueryable
        var filtered = tourRepo.Query(asNoTracking: true)
            .Where(t => t.Status == TourStatus.Approved && !t.IsDeleted);

        filtered = ApplyFilters(filtered, req);

        // 3. Apply token filter BEFORE facet snapshot and count
        //    AND semantics: all tokens must match in Name or Description
        if (tokens.Count > 0)
        {
            foreach (var token in tokens)
            {
                var t = token; // capture for closure
                filtered = filtered.Where(x =>
                    x.Name.Contains(t) ||
                    (x.Description != null && x.Description.Contains(t)));
            }
        }

        // 4. Exact total for correct pagination (separate CountAsync — no cap)
        var total = await filtered.CountAsync(ct);

        // 5. Facet computation — capped at 5001 so we can detect approximation
        const int FacetSampleCap = 5000;
        var facetRowsRaw = await filtered
            .Select(t => new FacetRow(
                t.BasePrice.Amount,
                t.Difficulty.ToString(),
                t.AverageRating,
                t.IsChildFriendly,
                t.IsAccessible,
                t.IsInstantBooking))
            .Take(FacetSampleCap + 1)
            .ToListAsync(ct);

        var facetsAreApproximate = facetRowsRaw.Count > FacetSampleCap;
        if (facetsAreApproximate) facetRowsRaw.RemoveAt(FacetSampleCap);

        var facets = FacetComputer.Compute(facetRowsRaw);

        // 6. Sort + paging (SQL-level)
        var skip = (req.Page - 1) * req.PageSize;

        IQueryable<ContentTours.Domain.Entities.Tour> sorted = req.Sort switch
        {
            SearchSort.PriceAsc       => filtered.OrderBy(t => t.SalePrice ?? t.BasePrice.Amount),
            SearchSort.PriceDesc      => filtered.OrderByDescending(t => t.SalePrice ?? t.BasePrice.Amount),
            SearchSort.RatingDesc     => filtered.OrderByDescending(t => t.AverageRating).ThenByDescending(t => t.ReviewCount),
            SearchSort.PopularityDesc => filtered.OrderByDescending(t => t.BookingCount).ThenByDescending(t => t.CreatedAt),
            SearchSort.Newest         => filtered.OrderByDescending(t => t.CreatedAt),
            // Relevance + default: popularity as v1 proxy (no SQL scoring yet)
            _                         => filtered.OrderByDescending(t => t.BookingCount).ThenByDescending(t => t.CreatedAt),
        };

        var page = await sorted
            .Skip(skip)
            .Take(req.PageSize)
            .Select(t => new TourSummaryDto(
                t.Id, t.Name, t.Slug,
                t.BasePrice.Amount, t.Currency, t.SalePrice,
                t.AverageRating, t.ReviewCount, t.BookingCount,
                t.IsFeatured, t.Status.ToString(), t.CreatedAt))
            .ToListAsync(ct);

        var totalPages = req.PageSize > 0 ? (int)Math.Ceiling((double)total / req.PageSize) : 0;

        logger.LogDebug(
            "SearchTours q='{Q}' tokens={TokenCount} total={Total} page={Page}",
            req.Q, tokens.Count, total, req.Page);

        return Result.Success(new SearchToursResult(
            page, total, req.Page, req.PageSize, totalPages, facets, facetsAreApproximate, req));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<SearchToursResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private static bool HasAnyFilter(SearchToursRequest req) =>
        req.PlaceId.HasValue || req.PriceMin.HasValue || req.PriceMax.HasValue ||
        req.Difficulty is not null || req.DurationMinutesMin.HasValue || req.DurationMinutesMax.HasValue ||
        req.IsChildFriendly.HasValue || req.IsAccessible.HasValue || req.IsInstantBooking.HasValue ||
        req.HasDiscount.HasValue || req.MinRating.HasValue || req.LanguageCode is not null;

    private static IQueryable<ContentTours.Domain.Entities.Tour> ApplyFilters(
        IQueryable<ContentTours.Domain.Entities.Tour> q, SearchToursRequest req)
    {
        if (req.PlaceId.HasValue)
            q = q.Where(t => t.PlaceId == req.PlaceId.Value);

        if (req.PriceMin.HasValue)
            q = q.Where(t => t.BasePrice.Amount >= req.PriceMin.Value);

        if (req.PriceMax.HasValue)
            q = q.Where(t => t.BasePrice.Amount <= req.PriceMax.Value);

        if (req.Difficulty is not null && Enum.TryParse<Difficulty>(req.Difficulty, true, out var diff))
            q = q.Where(t => t.Difficulty == diff);

        if (req.DurationMinutesMin.HasValue)
            q = q.Where(t => t.DurationMinutes >= req.DurationMinutesMin.Value);

        if (req.DurationMinutesMax.HasValue)
            q = q.Where(t => t.DurationMinutes <= req.DurationMinutesMax.Value);

        if (req.IsChildFriendly.HasValue)
            q = q.Where(t => t.IsChildFriendly == req.IsChildFriendly.Value);

        if (req.IsAccessible.HasValue)
            q = q.Where(t => t.IsAccessible == req.IsAccessible.Value);

        if (req.IsInstantBooking.HasValue)
            q = q.Where(t => t.IsInstantBooking == req.IsInstantBooking.Value);

        // hasDiscount=true  → only tours with an active discount window now.
        // hasDiscount=false → only tours with NO active discount window now.
        // hasDiscount=null  → no filter (any).
        if (req.HasDiscount == true)
        {
            q = q.Where(t => t.DiscountValidFrom <= DateTime.UtcNow && t.DiscountValidTo > DateTime.UtcNow);
        }
        else if (req.HasDiscount == false)
        {
            q = q.Where(t => t.DiscountValidFrom == null || t.DiscountValidTo == null
                          || t.DiscountValidFrom > DateTime.UtcNow || t.DiscountValidTo <= DateTime.UtcNow);
        }

        if (req.MinRating.HasValue)
            q = q.Where(t => t.AverageRating >= req.MinRating.Value);

        return q;
    }
}
