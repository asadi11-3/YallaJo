using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Category.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<DeleteCategoryCommandHandler> logger)
    : ICommandHandler<DeleteCategoryCommand>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);

            if (category is null)
            {
                return Result.Failure(
                    new Error(
                        "Category.NotFound",
                        $"Category '{request.Id}' was not found."),
                    Outcome.NotFound);
            }

            // Prevent deletion when subcategories exist (avoids orphaned children)
            if (await categoryRepository.AnyAsync(c => c.ParentCategoryId == category.Id, cancellationToken))
            {
                return Result.Failure(
                    new Error(
                        "Category.HasChildren",
                        "Cannot delete a category that still has subcategories. Remove or reassign children first."),
                    Outcome.Invalid);
            }

            // CategoryDeletedDomainEvent is now raised inside Category.SoftDelete() (encapsulated).
            category.SoftDelete();

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Category.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentCoreCacheKeys.CategoriesTag, cancellationToken);

            logger.LogInformation("Category soft-deleted: {CategoryId}", request.Id);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error(
                    "Request.Cancelled",
                    "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
