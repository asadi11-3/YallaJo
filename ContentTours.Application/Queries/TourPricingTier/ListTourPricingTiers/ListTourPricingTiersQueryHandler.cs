using ContentTours.Application.Queries.TourPricingTier.Common;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourPricingTier.ListTourPricingTiers;

public sealed class ListTourPricingTiersQueryHandler(
    ITourRepository tourRepo,
    ITourPricingTierRepository tierRepo,
    ITourPricingTierTranslationRepository translationRepo,
    IRequestContext requestContext,
    ILogger<ListTourPricingTiersQueryHandler> logger)
    : IQueryHandler<ListTourPricingTiersQuery, IReadOnlyList<TourPricingTierDto>>
{
    public async Task<Result<IReadOnlyList<TourPricingTierDto>>> Handle(
        ListTourPricingTiersQuery query, CancellationToken ct)
    {
        try
        {
            var tour = await tourRepo.GetByIdAsync(query.TourId, ct);
            if (tour is null || tour.IsDeleted)
            {
                return Result<IReadOnlyList<TourPricingTierDto>>.Failure(
                   new Error("Tour.NotFound", $"Tour '{query.TourId}' was not found."),
                   Outcome.NotFound);
            }

            // Visibility — driven by the query record (NOT by ICurrentUser) so the cached
            // output and the cache key always agree on which tier set was returned.
            // The endpoint is responsible for stamping CallerUserId + IsAdmin onto the
            // query before dispatch (P1 #2 cache-key fix).
            var isElevated = query.IsAdmin
                || (query.CallerUserId.HasValue && tour.CreatedByUserId == query.CallerUserId.Value);

            // Non-elevated callers are forced to active-only regardless of what they asked for.
            var activeOnly = !isElevated || query.ActiveOnly;
            var languageCode = ResolveLanguageCode(query.LanguageCode, requestContext.AcceptLanguage);
            var neutralLanguageCode = GetNeutralLanguageCode(languageCode);

            var tiers = await tierRepo.Query(asNoTracking: true)
                .Where(t => t.TourId == query.TourId && (!activeOnly || t.IsActive))
                .OrderBy(t => t.Name)
                .ToListAsync(ct);

            var tierIds = tiers.Select(t => t.Id).ToList();

            List<ContentTours.Domain.Entities.TourPricingTierTranslation> translations;

            if (tierIds.Count == 0)
            {
                translations = [];
            }
            else
            {
                translations = await translationRepo.Query(asNoTracking: true)
                    .Where(t => tierIds.Contains(t.TourPricingTierId) &&
                        (t.LanguageCode == languageCode || t.LanguageCode == neutralLanguageCode))
                    .OrderByDescending(t => t.LanguageCode == languageCode)
                    .ToListAsync(ct);
            }

            var translationsByTierId = translations
                .GroupBy(t => t.TourPricingTierId)
                .ToDictionary(g => g.Key, g => g.First());

            var dtos = tiers
                .Select(t =>
                {
                    translationsByTierId.TryGetValue(t.Id, out var translation);

                    return new TourPricingTierDto(
                        t.Id,
                        t.TourId,
                        translation?.Name ?? t.Name,
                        translation?.Description ?? t.Description,
                        t.Price.Amount,
                        t.Currency,
                        t.ParticipantType,
                        t.MinParticipants,
                        t.MaxParticipants,
                        t.IsActive,
                        t.CreatedAt);
                })
                .ToList() as IReadOnlyList<TourPricingTierDto>;

            logger.LogDebug(
                "Listed {Count} pricing tiers for TourId={TourId} Language={LanguageCode}",
                dtos.Count,
                query.TourId,
                languageCode ?? "default");

            return Result.Success(dtos);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<IReadOnlyList<TourPricingTierDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private static string? ResolveLanguageCode(string? explicitLanguageCode, string? acceptLanguage)
    {
        if (!string.IsNullOrWhiteSpace(explicitLanguageCode))
        {
            return explicitLanguageCode.Trim().ToLowerInvariant();
        }

        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return null;
        }

        var first = acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(first))
        {
            return null;
        }

        return first.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0]
            .Trim()
            .ToLowerInvariant();
    }

    private static string? GetNeutralLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        var separatorIndex = languageCode.IndexOf('-');
        return separatorIndex > 0
            ? languageCode[..separatorIndex]
            : languageCode;
    }
}
