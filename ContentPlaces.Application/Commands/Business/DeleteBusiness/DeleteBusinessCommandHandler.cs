using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Exceptions;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.Business.DeleteBusiness;

public sealed class DeleteBusinessCommandHandler(
    IBusinessRepository businessRepository,
    IContentPlacesUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<DeleteBusinessCommandHandler> logger)
    : ICommandHandler<DeleteBusinessCommand>
{
    public async Task<Result> Handle(DeleteBusinessCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var business = await businessRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);
            if (business is null)
            {
                return Result.Failure(
                    new Error("Business.NotFound", $"Business '{request.Id}' was not found."),
                    Outcome.NotFound);
            }

            business.SoftDelete();

            var saveResult = await SaveAsync(request.Id, cancellationToken);
            if (saveResult is not null)
                return saveResult;

            await cache.RemoveByTagAsync($"biz:{request.Id}", cancellationToken);
            await cache.RemoveByTagAsync("businesses", cancellationToken);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private async Task<Result?> SaveAsync(Guid businessId, CancellationToken ct)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            return null;
        }
        catch (ContentPlacesConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict deleting business '{BusinessId}'.", businessId);
            return Result.Failure(
                new Error("Business.ConcurrencyConflict", "The record was modified by another request. Please retry."),
                Outcome.Conflict);
        }
    }
}
