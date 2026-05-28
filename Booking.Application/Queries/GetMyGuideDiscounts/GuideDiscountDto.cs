using Booking.Domain.Enums;

namespace Booking.Application.Queries.GetMyGuideDiscounts;

public sealed record GuideDiscountDto(
    Guid Id,
    Guid? TourId,
    string Name,
    string? Description,
    GuideDiscountType DiscountType,
    decimal DiscountValue,
    string Currency,
    DateTime ValidFrom,
    DateTime? ValidUntil,
    int? MaxUsageCount,
    int CurrentUsageCount,
    bool IsActive);
