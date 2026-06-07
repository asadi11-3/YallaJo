using System.Globalization;
using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Application.Queries.BusinessAmenity.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using AmenityEntity = ContentPlaces.Domain.Entities.BusinessAmenity;

namespace ContentPlaces.Application.Commands.BusinessAmenity.AddBusinessAmenity;

public sealed class AddBusinessAmenityCommandHandler(
    IBusinessAmenityRepository amenityRepository,
    IBusinessRepository businessRepository,
    IContentPlacesUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<AddBusinessAmenityCommandHandler> logger)
    : ICommandHandler<AddBusinessAmenityCommand, BusinessAmenityDto>
{
    public async Task<Result<BusinessAmenityDto>> Handle(
        AddBusinessAmenityCommand request,
        CancellationToken cancellationToken)
    {
        var business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

        if (business is null)
        {
            return Result<BusinessAmenityDto>.Failure(
                new Error("Business.NotFound", "Business not found"),
                Outcome.NotFound);
        }

        if (business.OwnerId != request.ActingUserId)
        {
            return Result<BusinessAmenityDto>.Failure(
                new Error("Business.Forbidden", "Not the owner"),
                Outcome.Forbidden);
        }

        var normalizedName = request.Name.Trim().ToLower(CultureInfo.InvariantCulture);
        var exists = await amenityRepository.AnyAsync(
            x => x.BusinessId == request.BusinessId &&
                 string.Equals(x.Name, normalizedName, StringComparison.OrdinalIgnoreCase), cancellationToken);

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
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<BusinessAmenityDto>.Failure(
                new Error(
                    "BusinessAmenity.ConcurrencyConflict",
                    "A concurrency conflict occurred. Please refresh and try again."),
                Outcome.Conflict);
        }

        // Evict scoped business tag so ListBusinessAmenitiesQuery returns fresh data.
        await cache.RemoveByTagAsync(ContentPlacesCacheKeys.BusinessTag(request.BusinessId), cancellationToken);

        logger.LogInformation(
            "Amenity {AmenityId} added to Business {BusinessId}",
            amenity.Id, request.BusinessId);
        return Result<BusinessAmenityDto>.Created(BusinessAmenityDto.From(amenity));
    }
}
