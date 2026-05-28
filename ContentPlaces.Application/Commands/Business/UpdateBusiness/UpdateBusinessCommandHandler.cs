using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
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
            var business = await businessRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);
            if (business is null)
            {
                return Result.Failure(
                    new Error("Business.NotFound", $"Business '{request.Id}' was not found."),
                    Outcome.NotFound);
            }

            // Ownership check (IDOR prevention).
            if (business.OwnerId != currentUser.UserId!.Value)
            {
                return Result.Failure(
                    Error.Forbidden("You do not have permission to update this business."),
                    Outcome.Forbidden);
            }

            var oldPlaceId = business.PlaceId;

            // PlaceId is required; validate it exists
            if (request.PlaceId != oldPlaceId)
            {
                if (!await businessRepository.PlaceExistsAsync(request.PlaceId, cancellationToken))
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
                country: request.Country,
                isHalal: request.IsHalal,
                hasVegetarianOptions: request.HasVegetarianOptions,
                hasAlcoholFreeArea: request.HasAlcoholFreeArea);

            business.UpdateContactInfo(request.Phone, request.Email, request.Website);

            if (request.PlaceId != oldPlaceId)
            {
                // Remove old junction if it existed
                await businessRepository.RemovePlaceBusinessJunctionAsync(oldPlaceId, business.Id, cancellationToken);
                // Add new junction
                await businessRepository.AddPlaceBusinessJunctionAsync(request.PlaceId, business.Id, cancellationToken);
            }

            var saveResult = await SaveAsync(request.Id, cancellationToken);
            if (saveResult is not null)
                return saveResult;

            // Evict this business detail + all lists that include it
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
            logger.LogWarning(ex, "Concurrency conflict updating business '{BusinessId}'.", businessId);
            return Result.Failure(
                new Error("Business.ConcurrencyConflict", "The record was modified by another request. Please retry."),
                Outcome.Conflict);
        }
    }
}
