using ContentPlaces.Application.Interfaces;
using ContentPlaces.Application.Queries.BusinessAmenity.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using AmenityEntity = ContentPlaces.Domain.Entities.BusinessAmenity;

namespace ContentPlaces.Application.Commands.BusinessAmenity.AddBusinessAmenity;

public sealed class AddBusinessAmenityCommandHandler(
    IBusinessAmenityRepository amenityRepository,
    IBusinessRepository businessRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<AddBusinessAmenityCommandHandler> logger)
    : ICommandHandler<AddBusinessAmenityCommand, BusinessAmenityDto>
{
    public async Task<Result<BusinessAmenityDto>> Handle(
        AddBusinessAmenityCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<BusinessAmenityDto>.Failure(
                new Error("Auth.Unauthorized", "Authentication required"),
                Outcome.Unauthorized);
        }

        var business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

        if (business is null)
        {
            return Result<BusinessAmenityDto>.Failure(
                new Error("Business.NotFound", "Business not found"),
                Outcome.NotFound);
        }

        if (business.OwnerId != currentUser.UserId)
        {
            return Result<BusinessAmenityDto>.Failure(
                new Error("Auth.Forbidden", "Not allowed"),
                Outcome.Forbidden);
        }

        var normalizedName = request.Name.Trim().ToLower();

        var exists = await amenityRepository.AnyAsync(
            x => x.BusinessId == request.BusinessId &&
                 x.Name.ToLower() == normalizedName,
            cancellationToken);

        if (exists)
        {
            return Result<BusinessAmenityDto>.Failure(
                new Error("BusinessAmenity.Duplicate", "Amenity already exists"),
                Outcome.Conflict);
        }

        var amenity = AmenityEntity.Create(
            request.BusinessId,
            request.Name,
            request.Icon,
            request.SortOrder);

        await amenityRepository.AddAsync(amenity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Amenity {AmenityId} added to Business {BusinessId}",
            amenity.Id, request.BusinessId);

        return Result<BusinessAmenityDto>.Created(BusinessAmenityDto.From(amenity));
    }
}
