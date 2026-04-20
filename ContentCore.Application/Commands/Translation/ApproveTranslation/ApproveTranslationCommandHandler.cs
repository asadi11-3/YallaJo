using ContentCore.Domain.Exceptions;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Translation.ApproveTranslation;

public sealed class ApproveTranslationCommandHandler(
    ITranslationCacheRepository translationCacheRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<ApproveTranslationCommandHandler> logger)
    : ICommandHandler<ApproveTranslationCommand>
{
    public async Task<Result> Handle(
        ApproveTranslationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cached = await translationCacheRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);
            if (cached is null) {
                return Result.Failure(
                   new Error("Translation.NotFound", "Translation was not found."),
                   Outcome.NotFound);
            }

           
            cached.Approve();

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ContentCoreConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Translation.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }
   
            if (cached.EntityType is not null && cached.EntityId.HasValue)
            {
                await cache.RemoveByTagAsync(
                    $"translations:{cached.EntityType}:{cached.EntityId.Value}",
                    cancellationToken);
            }

            logger.LogInformation(
                "Translation approved: {TranslationId}", request.Id);

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
