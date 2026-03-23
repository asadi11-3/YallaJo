using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Category.ReactivateCategory;

public sealed class ReactivateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<ReactivateCategoryCommand, ReactivateCategoryResult>
{
    public async Task<Result<ReactivateCategoryResult>> Handle(
        ReactivateCategoryCommand request,
        CancellationToken ct)
    {
        try
        {
            var category = await categoryRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
            if (category is null)
                return Result<ReactivateCategoryResult>.Failure(
                    new Error("Category.NotFound", $"Category '{request.Id}' was not found."),
                    Outcome.NotFound);

            category.Activate();

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<ReactivateCategoryResult>.Conflict(
                    new Error("Category.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync("categories", ct);

            return Result<ReactivateCategoryResult>.Success(
                new ReactivateCategoryResult(category.Id, category.IsActive));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<ReactivateCategoryResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
