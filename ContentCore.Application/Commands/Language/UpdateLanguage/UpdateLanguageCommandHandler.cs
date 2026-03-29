using ContentCore.Application.Caching;
using ContentCore.Domain.Exceptions;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Language.UpdateLanguage;

public sealed class UpdateLanguageCommandHandler(
    ILanguageRepository languageRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<UpdateLanguageCommand, UpdateLanguageResult>
{
    public async Task<Result<UpdateLanguageResult>> Handle(
        UpdateLanguageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var language = await languageRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);
            if (language is null) {
                return Result<UpdateLanguageResult>.Failure(
                  new Error("Language.NotFound", $"Language '{request.Id}' was not found."),
                  Outcome.NotFound);
            }

            language.Update(request.Name, request.NativeName, request.IsRtl);

            if (request.IsActive)
                language.Activate();
            else
                language.Deactivate();

            languageRepository.Update(language);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ContentCoreConcurrencyException)
            {
                return Result<UpdateLanguageResult>.Conflict(
                    new Error(
                        "Language.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync("languages", cancellationToken);

            return Result<UpdateLanguageResult>.Success(
                new UpdateLanguageResult(language.Id, language.Name, language.NativeName, language.IsRtl, language.IsActive));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<UpdateLanguageResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
