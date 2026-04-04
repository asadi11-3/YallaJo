using ContentCore.Domain.Exceptions;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Category.DeactivateCategory;

public sealed class DeactivateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<DeactivateCategoryCommand, DeactivateCategoryResult>
{
    public async Task<Result<DeactivateCategoryResult>> Handle(
        DeactivateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);
            if (category is null)
            {
                return Result<DeactivateCategoryResult>.Failure(
                  new Error(
                      "Category.NotFound",
                      $"Category '{request.Id}' was not found."),
                  Outcome.NotFound);
            }

            category.Deactivate();

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ContentCoreConcurrencyException)
            {
                return Result<DeactivateCategoryResult>.Conflict(
                    new Error(
                        "Category.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync("categories", cancellationToken);

            return Result<DeactivateCategoryResult>.Success(
                new DeactivateCategoryResult(category.Id, category.IsActive));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<DeactivateCategoryResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
