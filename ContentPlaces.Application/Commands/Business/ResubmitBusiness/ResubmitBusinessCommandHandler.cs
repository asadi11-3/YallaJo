using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.Business.ResubmitBusiness;

public sealed class ResubmitBusinessCommandHandler(
    IBusinessRepository businessRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<ResubmitBusinessCommandHandler> logger)
    : ICommandHandler<ResubmitBusinessCommand>
{
    public async Task<Result> Handle(ResubmitBusinessCommand request, CancellationToken cancellationToken)
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

            if (business.OwnerId != currentUser.UserId!.Value)
            {
                return Result.Failure(
                    Error.Forbidden("Only the business owner can resubmit."),
                    Outcome.Forbidden);
            }

            if (business.Status is not BusinessStatus.Rejected and not BusinessStatus.MoreDocsNeeded)
            {
                return Result.Failure(
                    new Error("Business.InvalidTransition",
                        $"Cannot resubmit a business with status {business.Status}."),
                    Outcome.Conflict);
            }

            if (business.ResubmitCount >= 3)
            {
                return Result.Failure(
                    new Error("Business.ResubmitLimitReached",
                        "Maximum re-application limit (3) has been reached."),
                    Outcome.Conflict);
            }

            business.Resubmit();

            var saveResult = await SaveAsync(request.Id, cancellationToken);
            if (saveResult is not null)
                return saveResult;

            await cache.RemoveByTagAsync(ContentPlacesCacheKeys.TagForBusiness(request.Id), cancellationToken);
            await cache.RemoveByTagAsync(ContentPlacesCacheKeys.TagBusinesses, cancellationToken);

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
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict resubmitting business '{BusinessId}'.", businessId);
            return Result.Failure(
                new Error("Business.ConcurrencyConflict", "The record was modified by another request. Please retry."),
                Outcome.Conflict);
        }
    }
}
