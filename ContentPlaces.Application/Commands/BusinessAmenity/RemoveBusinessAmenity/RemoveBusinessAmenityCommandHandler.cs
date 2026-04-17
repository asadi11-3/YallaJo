using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Exceptions;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.BusinessAmenity.RemoveBusinessAmenity;

public sealed class RemoveBusinessAmenityCommandHandler(
    IBusinessAmenityRepository amenityRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<RemoveBusinessAmenityCommandHandler> logger)
    : ICommandHandler<RemoveBusinessAmenityCommand>
{
    public async Task<Result> Handle(
        RemoveBusinessAmenityCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return Result.Failure(
                new Error("Auth.Unauthorized", "Authentication required"),
                Outcome.Unauthorized);
        }

        var amenity = await amenityRepository.GetByIdAsync(request.AmenityId, cancellationToken, asNoTracking: false);

        if (amenity is null)
        {
            return Result.Failure(
                new Error("BusinessAmenity.NotFound", "Amenity not found"),
                Outcome.NotFound);
        }

        amenityRepository.Remove(amenity);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ContentPlaceConcurrencyException)
        {
            return Result.Failure(
                new Error(
                    "BusinessAmenity.ConcurrencyConflict",
                    "A concurrency conflict occurred. Please refresh and try again."),
                Outcome.Conflict);
        }

        logger.LogInformation("Amenity removed {AmenityId}", request.AmenityId);

        return Result.Success();
    }
}
