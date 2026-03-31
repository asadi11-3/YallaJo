using ContentCore.Domain.Exceptions;
using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Category.ReorderCategories;

public sealed class ReorderCategoriesCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<ReorderCategoriesCommand>
{
    public async Task<Result> Handle(
        ReorderCategoriesCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var ids = request.SortOrders.Select(x => x.CategoryId).ToList();

            var categories = await categoryRepository.GetAllAsync(
                filter: c => ids.Contains(c.Id),
                asNoTracking: false,
                ct: cancellationToken);

            var orderMap = request.SortOrders.ToDictionary(x => x.CategoryId, x => x.SortOrder);

            foreach (var category in categories)
            {
                if (orderMap.TryGetValue(category.Id, out var newOrder))
                    category.SetSortOrder(newOrder);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ContentCoreConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Category.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

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
