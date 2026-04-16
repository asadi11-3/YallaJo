using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Exceptions;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.Business.SuspendBusiness;

public sealed class SuspendBusinessCommandHandler(
    IBusinessRepository businessRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<SuspendBusinessCommandHandler> logger)
    : ICommandHandler<SuspendBusinessCommand>
{
    public async Task<Result> Handle(SuspendBusinessCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result.Failure(Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);
            }

            var business = await businessRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);
            if (business is null)
            {
                return Result.Failure(
                    new Error("Business.NotFound", $"Business '{request.Id}' was not found."),
                    Outcome.NotFound);
            }

            if (business.Status != BusinessStatus.Approved)
            {
                return Result.Failure(
                    new Error("Business.InvalidTransition",
                        $"Cannot suspend a business with status {business.Status}."),
                    Outcome.Conflict);
            }

            business.Suspend(request.Reason);

            var saveResult = await SaveAsync(request.Id, cancellationToken);
            if (saveResult is not null)
                return saveResult;

            // Status change: business disappears from public list — evict everything
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
            logger.LogWarning(ex, "Concurrency conflict suspending business '{BusinessId}'.", businessId);
            return Result.Failure(
                new Error("Business.ConcurrencyConflict", "The record was modified by another request. Please retry."),
                Outcome.Conflict);
        }
    }
}
