using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Language.DeleteLanguage;

public sealed class DeleteLanguageCommandHandler(
    ILanguageRepository languageRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<DeleteLanguageCommandHandler> logger)
    : ICommandHandler<DeleteLanguageCommand>
{
    public async Task<Result> Handle(DeleteLanguageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var language = await languageRepository.GetByIdAsync(
                request.Id, cancellationToken, asNoTracking: false);

            if (language is null)
            {
                return Result.Failure(
                   new Error("Language.NotFound", $"Language '{request.Id}' was not found."),
                   Outcome.NotFound);
            }

            // Language is AuditableEntity — soft-delete preserves history.
            language.SoftDelete();

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

            logger.LogInformation("Language {LanguageId} soft-deleted.", language.Id);

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
