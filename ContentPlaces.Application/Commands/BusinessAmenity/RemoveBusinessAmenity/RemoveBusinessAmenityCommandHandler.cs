using ContentPlaces.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.BusinessAmenity.RemoveBusinessAmenity;

public sealed class RemoveBusinessAmenityCommandHandler(
    IContentPlacesDbContext dbContext,
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

        var amenity = await dbContext.BusinessAmenities
            .FirstOrDefaultAsync(x => x.Id == request.AmenityId, cancellationToken);

        if (amenity is null)
        {
            return Result.Failure(
                new Error("BusinessAmenity.NotFound", "Amenity not found"),
                Outcome.NotFound);
        }

        dbContext.BusinessAmenities.Remove(amenity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Amenity removed {AmenityId}", request.AmenityId);

        return Result.Success();
    }
}
