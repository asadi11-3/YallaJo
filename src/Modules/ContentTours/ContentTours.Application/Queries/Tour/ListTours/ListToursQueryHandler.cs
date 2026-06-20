using ContentCore.Contracts.Attachments;
using ContentTours.Application.Queries.Tour.Common;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using TourEntity = ContentTours.Domain.Entities.Tour;

namespace ContentTours.Application.Queries.Tour.ListTours;

public sealed class ListToursQueryHandler(
    ITourRepository tourRepository,
    IActiveLanguageProvider activeLanguageProvider,
    IPublicEntityImageReader imageReader,
    ILogger<ListToursQueryHandler> logger)
    : IQueryHandler<ListToursQuery, PaginatedResult<TourSummaryDto>>
{
    private const int MaxPageSize = 50;
    private const string TourEntityType = "Tour";

    public async Task<Result<PaginatedResult<TourSummaryDto>>> Handle(
        ListToursQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            var targetLanguageId = await AcceptLanguageResolver.ResolveAsync(
                request.AcceptLanguage, activeLanguageProvider, cancellationToken)
                .ConfigureAwait(false);

            var paged = await tourRepository
                .SelectPaginatedAsync(
                    pageNumber: page,
                    pageSize:   pageSize,
                    selector:   t => new TourSummaryDto(
                        t.Id,
                        targetLanguageId == null
                            ? t.Name
                            : (t.TourTranslations
                                .Where(tt => tt.LanguageId == targetLanguageId)
                                .Select(tt => tt.Name)
                                .FirstOrDefault() ?? t.Name),
                        t.Slug,
                        t.BasePrice.Amount,
                        t.Currency,
                        t.SalePrice,
                        t.AverageRating,
                        t.ReviewCount,
                        t.BookingCount,
                        t.IsFeatured,
                        t.Status.ToString(),
                        t.CreatedAt,
                        // Expression trees cannot use optional args — pass null
                        // explicitly; PrimaryImageUrl is enriched after paging below.
                        null),
                    filter: t => t.Status == TourStatus.Approved
                              && (request.PlaceId == null || t.PlaceId == request.PlaceId)
                              && (request.IsFeatured == null || t.IsFeatured == request.IsFeatured.Value),
                    orderBy: q => OrderBy(q, request.Sort),
                    ct: cancellationToken)
                .ConfigureAwait(false);

            // Enrich card items with their real primary image in a SINGLE batched
            // ContentCore query (no N+1). EntityImages live in a separate DbContext,
            // so they cannot be joined into the EF projection above. Tours without an
            // image stay null and the Web layer falls back to a placeholder.
            var enriched = await EnrichWithPrimaryImagesAsync(paged.Items, cancellationToken)
                .ConfigureAwait(false);

            return Result<PaginatedResult<TourSummaryDto>>.Success(
                new PaginatedResult<TourSummaryDto>(
                    enriched, paged.TotalCount, paged.PageNumber, paged.PageSize));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<PaginatedResult<TourSummaryDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private async Task<IReadOnlyList<TourSummaryDto>> EnrichWithPrimaryImagesAsync(
        IReadOnlyList<TourSummaryDto> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return items;

        var tourIds = items.Select(i => i.Id).ToList();
        var imagesByTour = await imageReader
            .GetEntityImagesBatchAsync(TourEntityType, tourIds, cancellationToken)
            .ConfigureAwait(false);

        return items
            .Select(dto => imagesByTour.TryGetValue(dto.Id, out var images) && images.Count > 0
                // Batch reader orders IsPrimary-first then SortOrder, so the first
                // entry is the primary (or best) image for the card.
                ? dto with { PrimaryImageUrl = images[0].Url }
                : dto)
            .ToList();
    }

    private static IOrderedQueryable<TourEntity> OrderBy(IQueryable<TourEntity> query, string? sort) =>
        sort?.ToLowerInvariant() switch
        {
            "price_asc"       => query.OrderBy(t => t.BasePrice.Amount),
            "price_desc"      => query.OrderByDescending(t => t.BasePrice.Amount),
            "rating_desc"     => query.OrderByDescending(t => t.AverageRating)
                                      .ThenByDescending(t => t.ReviewCount),
            "popularity_desc" => query.OrderByDescending(t => t.BookingCount)
                                      .ThenByDescending(t => t.AverageRating),
            "newest"          => query.OrderByDescending(t => t.CreatedAt),
            _                 => query.OrderByDescending(t => t.IsFeatured)
                                      .ThenByDescending(t => t.AverageRating)
                                      .ThenByDescending(t => t.CreatedAt),
        };
}
