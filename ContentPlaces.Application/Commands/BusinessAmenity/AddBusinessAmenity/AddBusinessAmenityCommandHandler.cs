using ContentPlaces.Application.Interfaces;
using ContentPlaces.Application.Queries.BusinessAmenity.Common;
using ContentPlaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.BusinessAmenity.AddBusinessAmenity;

public sealed class AddBusinessAmenityCommandHandler(
    IContentPlacesDbContext dbContext,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<AddBusinessAmenityCommandHandler> logger)
    : ICommandHandler<AddBusinessAmenityCommand, BusinessAmenityDto>
{
    public async Task<Result<BusinessAmenityDto>> Handle(
        AddBusinessAmenityCommand request,
        CancellationToken cancellationToken)
    {
        // check authentication
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<BusinessAmenityDto>.Failure(
                new Error("Auth.Unauthorized", "Authentication required"),
                Outcome.Unauthorized);
        }

        // check business exists
        var business = await dbContext.Businesses
            .FirstOrDefaultAsync(x => x.Id == request.BusinessId, cancellationToken);

        if (business is null)
        {
            return Result<BusinessAmenityDto>.Failure(
                new Error("Business.NotFound", "Business not found"),
                Outcome.NotFound);
        }

        // check ownership (authorization)
        if (business.OwnerId != currentUser.UserId)
        {
            return Result<BusinessAmenityDto>.Failure(
                new Error("Auth.Forbidden", "Not allowed"),
                Outcome.Forbidden);
        }

        // normalize name
        var normalizedName = request.Name.Trim().ToLower();

        // prevent duplicates
        var exists = await dbContext.BusinessAmenities
            .AnyAsync(x => x.BusinessId == request.BusinessId &&
                           x.Name.ToLower() == normalizedName,
                      cancellationToken);

        if (exists)
        {
            return Result<BusinessAmenityDto>.Failure(
                new Error("BusinessAmenity.Duplicate", "Amenity already exists"),
                Outcome.Conflict);
        }

        // create entity
        var amenity = ContentPlaces.Domain.Entities.BusinessAmenity.Create(
            request.BusinessId,
            request.Name,
            request.Icon,
            request.SortOrder);

        await dbContext.BusinessAmenities.AddAsync(amenity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // better logging
        logger.LogInformation(
            "Amenity {AmenityId} added to Business {BusinessId}",
            amenity.Id,
            request.BusinessId);

        return Result<BusinessAmenityDto>.Created(BusinessAmenityDto.From(amenity));
    }
}
