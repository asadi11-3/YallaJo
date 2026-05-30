using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Category.RestoreCategory;

public sealed class RestoreCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<RestoreCategoryCommandHandler> logger)
    : ICommandHandler<RestoreCategoryCommand, RestoreCategoryResult>
{
    public async Task<Result<RestoreCategoryResult>> Handle(
        RestoreCategoryCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Must bypass soft-delete filter to find the deleted record
            var category = await categoryRepository.GetByIdIncludingDeletedAsync(request.Id, cancellationToken);

            if (category is null)
            {
                return Result<RestoreCategoryResult>.Failure(
                    new Error("Category.NotFound", $"Category '{request.Id}' was not found."),
                    Outcome.NotFound);
            }

            if (!category.IsDeleted)
            {
                return Result<RestoreCategoryResult>.Failure(
                    new Error("Category.NotDeleted", "Category is not deleted and cannot be restored."),
                    Outcome.Invalid);
            }

            // CategoryRestoredDomainEvent is now raised inside Category.Restore() (encapsulated).
            category.Restore();

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<RestoreCategoryResult>.Conflict(
                    new Error(
                        "Category.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync(ContentCoreCacheKeys.CategoriesTag, cancellationToken);

            logger.LogInformation("Category restored (un-deleted): {CategoryId}", request.Id);

            return Result<RestoreCategoryResult>.Success(
                new RestoreCategoryResult(category.Id, category.IsDeleted));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<RestoreCategoryResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
