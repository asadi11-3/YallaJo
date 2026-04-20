using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.Language.ListLanguages;

public sealed class ListLanguagesQueryHandler(
    ILanguageRepository languageRepository,
    ILogger<ListLanguagesQueryHandler> logger)
    : IQueryHandler<ListLanguagesQuery, IReadOnlyList<LanguageDto>>
{
    public async Task<Result<IReadOnlyList<LanguageDto>>> Handle(
        ListLanguagesQuery request,
        CancellationToken ct)
    {
        try
        {
            var languages = await languageRepository.GetAllAsync(
                filter: request.ActiveOnly ? l => l.IsActive : null,
                orderBy: q => q.OrderBy(l => l.Name),
                ct: ct);

            var dtos = languages
                .Select(l => new LanguageDto(l.Id, l.Code, l.Name, l.NativeName, l.IsRtl, l.IsActive))
                .ToList() as IReadOnlyList<LanguageDto>;

            logger.LogDebug("ListLanguages returned {Count} languages", dtos.Count);

            return Result<IReadOnlyList<LanguageDto>>.Success(dtos);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<IReadOnlyList<LanguageDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
