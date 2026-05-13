using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Translation.UpdateTranslation;

public sealed class UpdateTranslationCommandHandler(
    ITranslationCacheRepository translationCacheRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UpdateTranslationCommandHandler> logger)
    : ICommandHandler<UpdateTranslationCommand, UpdateTranslationResult>
{
    public async Task<Result<UpdateTranslationResult>> Handle(
        UpdateTranslationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cached = await translationCacheRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);
            if (cached is null) {
                return Result<UpdateTranslationResult>.Failure(
                    new Error("Translation.NotFound", "Translation was not found."),
                    Outcome.NotFound);
            }

            cached.UpdateTranslation(request.TranslatedText);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<UpdateTranslationResult>.Conflict(
                    new Error(
                        "Translation.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            if (cached.EntityType is not null && cached.EntityId.HasValue)
            {
                await cache.RemoveByTagAsync(
                    ContentCoreCacheKeys.EntityTranslationsTag(cached.EntityType, cached.EntityId.Value),
                    cancellationToken);
            }

            logger.LogInformation(
                "Translation updated: {TranslationId}", request.Id);

            return Result<UpdateTranslationResult>.Success(
                new UpdateTranslationResult(cached.Id, cached.TranslatedText, cached.Status.ToString()));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<UpdateTranslationResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
