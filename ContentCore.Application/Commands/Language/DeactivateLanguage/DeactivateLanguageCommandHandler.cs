using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Language.DeactivateLanguage;

public sealed class DeactivateLanguageCommandHandler(
    ILanguageRepository languageRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<DeactivateLanguageCommandHandler> logger)
    : ICommandHandler<DeactivateLanguageCommand>
{
    public async Task<Result> Handle(DeactivateLanguageCommand request, CancellationToken cancellationToken)
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

            if (!language.IsActive)
                return Result.Success();   // idempotent no-op

            language.Deactivate();

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

            logger.LogInformation("Language {LanguageId} deactivated.", language.Id);

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
