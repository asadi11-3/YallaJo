using ContentTours.Application.Queries.Tour.Common;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.Tour.SuggestTours;

/// <summary>
/// Resolves the requested language id once via <see cref="AcceptLanguageResolver"/>,
/// then runs a single SQL round-trip that prefix-matches <c>Tour.Name</c>
/// OR <c>TourTranslation.Name</c> for that language. Source-name match is the path
/// for English (or any tour whose source language equals the request); translation
/// match is the path for Arabic / any non-source language.
///
/// One row per Tour is emitted (the inner <c>.Any(...)</c> is an existence check, not
/// a join — natural deduplication). The displayed name is the matching translation
/// when present, else the source <c>Tour.Name</c>. Ordering uses the same display
/// expression so prefix matches sort by their visible value, not by source.
/// </summary>
public sealed class SuggestToursQueryHandler(
    ITourRepository tourRepo,
    IActiveLanguageProvider activeLanguageProvider,
    ILogger<SuggestToursQueryHandler> logger)
    : IQueryHandler<SuggestToursQuery, IReadOnlyList<TourSuggestDto>>
{
    public async Task<Result<IReadOnlyList<TourSuggestDto>>> Handle(
        SuggestToursQuery query, CancellationToken ct)
    {
        try
        {
            var prefix = query.Q.Trim();
            var pattern = prefix + "%";

            // Resolve the requested language to an active language id (with neutral
            // fallback). When null, only source-name matching applies.
            var languageId = await AcceptLanguageResolver
                .ResolveAsync(query.AcceptLanguage, activeLanguageProvider, ct)
                .ConfigureAwait(false);

            var dtos = await tourRepo
                .Query(asNoTracking: true)
                .Where(t =>
                    t.Status == TourStatus.Approved &&
                    !t.IsDeleted &&
                    (
                        EF.Functions.Like(t.Name, pattern) ||
                        (languageId != null && t.TourTranslations.Any(tt =>
                            tt.LanguageId == languageId.Value &&
                            EF.Functions.Like(tt.Name, pattern)))
                    ))
                .OrderByDescending(t => t.BookingCount)
                // Sort by the same display name the caller will see — Arabic translations
                // shown in Arabic order, untranslated tours by source name.
                .ThenBy(t => languageId == null
                    ? t.Name
                    : (t.TourTranslations
                        .Where(tt => tt.LanguageId == languageId.Value)
                        .Select(tt => tt.Name)
                        .FirstOrDefault() ?? t.Name))
                .Take(10)
                .Select(t => new TourSuggestDto(
                    t.Id,
                    languageId == null
                        ? t.Name
                        : (t.TourTranslations
                            .Where(tt => tt.LanguageId == languageId.Value)
                            .Select(tt => tt.Name)
                            .FirstOrDefault() ?? t.Name),
                    t.Slug,
                    null))
                .ToListAsync(ct);

            logger.LogDebug(
                "SuggestTours for q='{Q}' lang={LanguageId}: {Count} results",
                query.Q, languageId?.ToString() ?? "(none)", dtos.Count);

            return Result.Success((IReadOnlyList<TourSuggestDto>)dtos);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<IReadOnlyList<TourSuggestDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
