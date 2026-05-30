using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Language.ActivateLanguage;

public sealed class ActivateLanguageCommandHandler(
    ILanguageRepository languageRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<ActivateLanguageCommandHandler> logger)
    : ICommandHandler<ActivateLanguageCommand>
{
    public async Task<Result> Handle(ActivateLanguageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var language = await languageRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);
            if (language is null)
            {
                return Result.Failure(
                   new Error("Language.NotFound", $"Language '{request.Id}' was not found."),
                   Outcome.NotFound);
            }

            if (language.IsActive)
                return Result.Success();   // idempotent no-op

            language.Activate();

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Language.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentCoreCacheKeys.LanguagesTag, cancellationToken);
            await cache.RemoveByTagAsync(ContentCoreCacheKeys.LanguageTag(language.Id), cancellationToken);

            logger.LogInformation("Language {LanguageId} activated.", language.Id);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
