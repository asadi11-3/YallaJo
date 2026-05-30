using ContentCore.Application.Queries.Language.ListLanguages;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.Language.GetLanguageById;

public sealed class GetLanguageByIdQueryHandler(
    ILanguageRepository languageRepository,
    ILogger<GetLanguageByIdQueryHandler> logger)
    : IQueryHandler<GetLanguageByIdQuery, LanguageDto>
{
    public async Task<Result<LanguageDto>> Handle(
        GetLanguageByIdQuery request,
        CancellationToken ct)
    {
        try
        {
            var language = await languageRepository.GetByIdAsync(request.Id, ct);

            if (language is null)
                return Result<LanguageDto>.Failure(
                    new Error("Language.NotFound", $"Language '{request.Id}' was not found."),
                    Outcome.NotFound);

            logger.LogDebug("GetLanguageById: {LanguageId} (Code={Code})", language.Id, language.Code);

            return Result<LanguageDto>.Success(
                new LanguageDto(
                    language.Id,
                    language.Code,
                    language.Name,
                    language.NativeName,
                    language.IsRtl,
                    language.IsActive));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<LanguageDto>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
