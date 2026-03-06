using ContentCore.Application.Interfaces;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Queries.Translation.GetEntityTranslations;

public sealed class GetEntityTranslationsQueryHandler(ITranslationCacheRepository translationCacheRepository)
    : IQueryHandler<GetEntityTranslationsQuery, IReadOnlyList<EntityTranslationDto>>
{
    public async Task<Result<IReadOnlyList<EntityTranslationDto>>> Handle(
        GetEntityTranslationsQuery request,
        CancellationToken ct)
    {
        var translations = await translationCacheRepository.GetByEntityAsync(
            request.EntityType,
            request.EntityId,
            ct);

        var dtos = translations
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

        return Result<IReadOnlyList<EntityTranslationDto>>.Success(dtos);
    }
}
