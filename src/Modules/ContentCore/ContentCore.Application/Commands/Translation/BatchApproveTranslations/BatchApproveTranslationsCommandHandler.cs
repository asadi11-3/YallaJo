using ContentCore.Application.Caching;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Translation.BatchApproveTranslations;

/// <summary>
/// Marks all auto-translated fields as HumanReviewed in one save for a given
/// entity + language pair. Optionally restricted to specific field names.
/// </summary>
public sealed class BatchApproveTranslationsCommandHandler(
    ITranslationCacheRepository translationCacheRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<BatchApproveTranslationsCommandHandler> logger)
    : ICommandHandler<BatchApproveTranslationsCommand, BatchApproveTranslationsResult>
{
    public async Task<Result<BatchApproveTranslationsResult>> Handle(
        BatchApproveTranslationsCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var translations = await translationCacheRepository.GetByEntityAsync(
                request.EntityType,
                request.EntityId,
                cancellationToken);

            // Filter to the requested language
            var candidates = translations
                .Where(t => string.Equals(
                    t.ToLanguage, request.LanguageCode, StringComparison.OrdinalIgnoreCase))
                .Where(t => t.Status == TranslationStatus.AutoTranslated);

            // Optionally restrict to specific fields
            if (request.FieldNames is { Count: > 0 })
            {
                var fieldSet = new HashSet<string>(request.FieldNames, StringComparer.OrdinalIgnoreCase);
                candidates = candidates.Where(t => t.FieldName is not null && fieldSet.Contains(t.FieldName));
            }

            var toApprove = candidates.ToList();

            if (toApprove.Count == 0)
            {
                return Result<BatchApproveTranslationsResult>.Success(
                    new BatchApproveTranslationsResult(0));
            }

            foreach (var translation in toApprove)
                translation.Approve();

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<BatchApproveTranslationsResult>.Conflict(
                    new Error(
                        "Translation.ConcurrencyConflict",
                        "One or more records were modified by another user. Please retry."));
            }

            // Invalidate cache for this entity's translations
            await cache.RemoveByTagAsync(
                ContentCoreCacheKeys.EntityTranslationsTag(request.EntityType, request.EntityId),
                cancellationToken);

            logger.LogInformation(
                "BatchApproveTranslations: approved {Count} translations for {EntityType}/{EntityId} language={LanguageCode}",
                toApprove.Count, request.EntityType, request.EntityId, request.LanguageCode);

            return Result<BatchApproveTranslationsResult>.Success(
                new BatchApproveTranslationsResult(toApprove.Count));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<BatchApproveTranslationsResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
