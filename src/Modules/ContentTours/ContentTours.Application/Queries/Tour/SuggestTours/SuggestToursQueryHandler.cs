using ContentTours.Application.Queries.Tour.Common;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.Tour.SuggestTours;

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
