using ContentPlaces.Application.Queries.Business.Common;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.Business.GetBusinessById;

public sealed class GetBusinessByIdQueryHandler(
    IBusinessRepository businessRepository,
    ILogger<GetBusinessByIdQueryHandler> logger)
    : IQueryHandler<GetBusinessByIdQuery, BusinessDetailDto>
{
    public async Task<Result<BusinessDetailDto>> Handle(
        GetBusinessByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var business = await businessRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken);

            if (business is null)
            {
                return Result<BusinessDetailDto>.Failure(
                    new Error("Business.NotFound", $"Business '{request.Id}' was not found or you do not have permission to view it."),
                    Outcome.NotFound);
            }

            // Public callers can only see Approved businesses
            var isOwnerOrAdmin = request.IsAdmin || (request.UserId.HasValue && business.OwnerId == request.UserId.Value);
            if (business.Status != BusinessStatus.Approved && !isOwnerOrAdmin)
            {
                return Result<BusinessDetailDto>.Failure(
                    new Error("Business.NotFound", $"Business '{request.Id}' was not found or you do not have permission to view it."),
                    Outcome.NotFound);
            }

            // Only owners/admins may see the rejection reason
            var rejectionReason = isOwnerOrAdmin ? business.RejectionReason : null;

            var translations = business.BusinessTranslations
                .Select(t => new BusinessTranslationDto(t.LanguageId, t.Name, t.Description, t.Address))
                .ToList();

            var hours = business.BusinessHours
                .OrderBy(h => h.DayOfWeek)
                .ThenBy(h => h.OpenTime)
                .Select(h => new BusinessHoursDto(
                    h.Id,
                    h.DayOfWeek.ToString(),
                    h.IsClosed ? null : h.OpenTime.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                    h.IsClosed ? null : h.CloseTime.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                    h.IsClosed))
                .ToList();

            // CONTENTPLACES-FOLLOWUP-DTO-REDACTION-001: OwnerId, LicenseNumber and
            // TaxId are intentionally NOT projected onto the public DTO. They have
            // been removed from BusinessDetailDto entirely — admin/management UI
            // must use a future protected management query/DTO.
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
                Status: business.Status.ToString(),
                RejectionReason: rejectionReason,
                MetaTitle: business.MetaTitle,
                MetaDescription: business.MetaDescription,
                SubscriptionTier: business.SubscriptionTier?.ToString(),
                ServiceItemCount: business.ServiceItems.Count,
                StaffCount: business.Staff.Count,
                AmenityCount: business.Amenities.Count,
                Translations: translations,
                BusinessHours: hours,
                CreatedAt: business.CreatedAt,
                UpdatedAt: business.UpdatedAt);

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
