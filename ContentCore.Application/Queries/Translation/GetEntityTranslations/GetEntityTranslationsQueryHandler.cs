using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.Translation.GetEntityTranslations;

public sealed class GetEntityTranslationsQueryHandler(
    ITranslationCacheRepository translationCacheRepository,
    ILogger<GetEntityTranslationsQueryHandler> logger)
    : IQueryHandler<GetEntityTranslationsQuery, IReadOnlyList<EntityTranslationDto>>
{
    public async Task<Result<IReadOnlyList<EntityTranslationDto>>> Handle(
        GetEntityTranslationsQuery request,
        CancellationToken ct)
    {
        try
        {
            var translations = await translationCacheRepository.GetByEntityAsync(
                request.EntityType,
                request.EntityId,
                ct);

            var filtered = translations.AsEnumerable();

            if (request.LanguageCode is not null)
                filtered = filtered.Where(t =>
                    string.Equals(t.ToLanguage, request.LanguageCode, StringComparison.OrdinalIgnoreCase));

            if (request.Status is not null)
                filtered = filtered.Where(t => t.Status == request.Status.Value);

            var dtos = filtered
                .Select(t => new EntityTranslationDto(
                    t.Id,
                    t.OriginalText,
                    t.TranslatedText,
                    t.FromLanguage,
                    t.ToLanguage,
                    t.FieldName,
                    t.Status.ToString(),
                    t.Confidence,
                    t.CreatedAt))
                .ToList() as IReadOnlyList<EntityTranslationDto>;

            logger.LogDebug(
                "GetEntityTranslations: {Count} translations for {EntityType}/{EntityId}",
                dtos.Count, request.EntityType, request.EntityId);

            return Result<IReadOnlyList<EntityTranslationDto>>.Success(dtos);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<IReadOnlyList<EntityTranslationDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
