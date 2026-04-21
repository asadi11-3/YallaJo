using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentPlaces.Application.Commands.Business.UpdateBusiness;

public sealed class UpdateBusinessCommandHandler(
    IBusinessRepository businessRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<UpdateBusinessCommandHandler> logger)
    : ICommandHandler<UpdateBusinessCommand>
{
    public async Task<Result> Handle(UpdateBusinessCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
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

            var isAdmin = currentUser.IsInRole("Admin");
            if (!isAdmin && business.OwnerId != currentUser.UserId.Value)
            {
                return Result.Failure(
                    Error.Forbidden("You do not have permission to update this business."),
                    Outcome.Forbidden);
            }

            var oldPlaceId = business.PlaceId;

            if (request.PlaceId.HasValue && request.PlaceId != oldPlaceId)
            {
                if (!await businessRepository.PlaceExistsAsync(request.PlaceId.Value, cancellationToken))
                {
                    return Result.Failure(
                        new Error("Place.NotFound", $"Place '{request.PlaceId}' was not found or has been deleted."),
                        Outcome.NotFound);
                }
            }

            business.Update(
                name: request.Name,
                description: request.Description,
                location: new Location(request.Latitude, request.Longitude),
                placeId: request.PlaceId,
                address: request.Address,
                city: request.City,
                country: request.Country);

            business.UpdateContactInfo(request.Phone, request.Email, request.Website);

            if (request.PlaceId != oldPlaceId)
            {
                if (oldPlaceId.HasValue)
                {
                    await businessRepository.RemovePlaceBusinessJunctionAsync(oldPlaceId.Value, business.Id, cancellationToken);
                }

                if (request.PlaceId.HasValue)
                {
                    await businessRepository.AddPlaceBusinessJunctionAsync(request.PlaceId.Value, business.Id, cancellationToken);
                }
            }

            var saveResult = await SaveAsync(request.Id, cancellationToken);
            if (saveResult is not null)
                return saveResult;

            // Evict this business detail + all lists that include it
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
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict updating business '{BusinessId}'.", businessId);
            return Result.Failure(
                new Error("Business.ConcurrencyConflict", "The record was modified by another request. Please retry."),
                Outcome.Conflict);
        }
    }
}
