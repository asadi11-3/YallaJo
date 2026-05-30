using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.Business.RequestMoreDocs;

public sealed class RequestMoreDocsCommandHandler(
    IBusinessRepository businessRepository,
    IContentPlacesUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<RequestMoreDocsCommandHandler> logger)
    : ICommandHandler<RequestMoreDocsCommand>
{
    public async Task<Result> Handle(RequestMoreDocsCommand request, CancellationToken cancellationToken)
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

            if (business.Status is not BusinessStatus.Pending and not BusinessStatus.MoreDocsNeeded)
            {
                return Result.Failure(
                    new Error("Business.InvalidTransition",
                        $"Cannot request more docs for a business with status {business.Status}."),
                    Outcome.Conflict);
            }

            business.RequestMoreDocs(request.Reason, request.ReviewedByUserId);

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
            logger.LogWarning(ex, "Concurrency conflict requesting more docs for business '{BusinessId}'.", businessId);
            return Result.Failure(
                new Error("Business.ConcurrencyConflict", "The record was modified by another request. Please retry."),
                Outcome.Conflict);
        }
    }
}
