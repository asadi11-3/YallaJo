using ContentPlaces.Application.Queries.Business.Common;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.Business.GetBusinessById
{
    public sealed class GetBusinessByIdQueryHandler(
    IBusinessRepository businessRepository)
    : IQueryHandler<GetBusinessByIdQuery, BusinessDetailDto>
    {
        public async Task<Result<BusinessDetailDto>> Handle(
            GetBusinessByIdQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var business = await businessRepository.FirstOrDefaultAsync(
                    filter: b => b.Id == request.Id &&
                                 (b.Status == BusinessStatus.Approved ||
                                  request.IsAdmin ||
                                  (request.UserId.HasValue && b.OwnerId == request.UserId.Value)),
                    ct: cancellationToken);

                if (business is null)
                {
                    return Result<BusinessDetailDto>.Failure(
                        new Error("Business.NotFound", $"Business '{request.Id}' was not found or you do not have permission to view it."),
                        Outcome.NotFound);
                }

                // Security Rule: Mask sensitive data (like RejectionReason) if the user is not the owner or an admin
                var isOwnerOrAdmin = request.IsAdmin || (request.UserId.HasValue && business.OwnerId == request.UserId.Value);
                var rejectionReason = isOwnerOrAdmin ? business.RejectionReason : null;

                var dto = new BusinessDetailDto(
                    Id: business.Id,
                    Name: business.Name,
                    Slug: business.Slug,
                    Description: business.Description,
                    BusinessType: business.BusinessType.ToString(),
                    PlaceId: business.PlaceId,
                    Latitude: business.Location.Latitude,
                    Longitude: business.Location.Longitude,
                    Address: business.Address,
                    City: business.City,
                    Country: business.Country,
                    PostalCode: business.PostalCode,
                    Phone: business.Phone,
                    Email: business.Email,
                    Website: business.Website,
                    AverageRating: business.AverageRating,
                    ReviewCount: business.ReviewCount,
                    IsVerified: business.IsVerified,
                    IsFeatured: business.IsFeatured,
                    OwnerId: business.OwnerId,
                    Status: business.Status.ToString(),
                    RejectionReason: rejectionReason,
                    CreatedAt: business.CreatedAt);

                return Result<BusinessDetailDto>.Success(dto);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return Result<BusinessDetailDto>.Failure(
                    new Error("Request.Cancelled", "The request was cancelled."),
                    Outcome.Canceled);
            }
        }
    }
}
