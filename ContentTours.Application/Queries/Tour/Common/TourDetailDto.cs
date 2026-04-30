using ContentTours.Domain.Enums;
using TourEntity = ContentTours.Domain.Entities.Tour;

namespace ContentTours.Application.Queries.Tour.Common;

public sealed record TourDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ShortDescription,
    Difficulty Difficulty,
    int DurationMinutes,
    int MaxGroupSize,
    int? MinAge,
    decimal BasePrice,
    string Currency,
    decimal? SalePrice,
    string? SalePriceCurrency,
    decimal? DiscountPercent,
    DateTime? DiscountValidFrom,
    DateTime? DiscountValidTo,
    decimal Latitude,
    decimal Longitude,
    decimal? MeetingPointLatitude,
    decimal? MeetingPointLongitude,
    TourStatus Status,
    decimal AverageRating,
    int ReviewCount,
    int BookingCount,
    bool IsFeatured,
    bool IsInstantBooking,
    int CancellationPolicyHours,
    bool IsChildFriendly,
    bool IsAccessible,
    int? AgeRestriction,
    string? MetaTitle,
    string? MetaDescription,
    Guid CreatedByUserId,
    Guid? PlaceId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt,
    Guid? ApprovedByUserId,
    DateTime? RejectedAt,
    Guid? RejectedByUserId,
    string? RejectionReason,
    DateTime? SuspendedAt,
    string? SuspensionReason,
    DateTime? ReinstatedAt,
    IReadOnlyList<TourTranslationDto> Translations)
{
    /// <summary>
    /// Projects an aggregate to the full detail DTO. Caller is responsible for
    /// suppressing approval-audit and PII fields when visibility is anonymous.
    /// When <paramref name="preferredLanguageId"/> is supplied and a matching
    /// <c>TourTranslation</c> exists, the translated <c>Name</c> /
    /// <c>Description</c> / <c>ShortDescription</c> / <c>MeetingPoint</c> are
    /// returned with <c>COALESCE(translation, source)</c> fallback.
    /// </summary>
    public static TourDetailDto From(TourEntity tour, Guid? preferredLanguageId = null)
    {
        var translation = preferredLanguageId.HasValue
            ? tour.TourTranslations.FirstOrDefault(t => t.LanguageId == preferredLanguageId.Value)
            : null;

        return new TourDetailDto(
        Id:                       tour.Id,
        Name:                     translation?.Name ?? tour.Name,
        Slug:                     tour.Slug,
        Description:              translation?.Description ?? tour.Description,
        ShortDescription:         translation?.ShortDescription ?? tour.ShortDescription,
        Difficulty:               tour.Difficulty,
        DurationMinutes:          tour.DurationMinutes,
        MaxGroupSize:             tour.MaxGroupSize,
        MinAge:                   tour.MinAge,
        BasePrice:                tour.BasePrice.Amount,
        Currency:                 tour.Currency,
        SalePrice:                tour.SalePrice,
        SalePriceCurrency:        tour.SalePriceCurrency,
        DiscountPercent:          tour.DiscountPercent,
        DiscountValidFrom:        tour.DiscountValidFrom,
        DiscountValidTo:          tour.DiscountValidTo,
        Latitude:                 tour.Location.Latitude,
        Longitude:                tour.Location.Longitude,
        MeetingPointLatitude:     tour.MeetingPoint?.Latitude,
        MeetingPointLongitude:    tour.MeetingPoint?.Longitude,
        Status:                   tour.Status,
        AverageRating:            tour.AverageRating,
        ReviewCount:              tour.ReviewCount,
        BookingCount:             tour.BookingCount,
        IsFeatured:               tour.IsFeatured,
        IsInstantBooking:         tour.IsInstantBooking,
        CancellationPolicyHours:  tour.CancellationPolicyHours,
        IsChildFriendly:          tour.IsChildFriendly,
        IsAccessible:             tour.IsAccessible,
        AgeRestriction:           tour.AgeRestriction,
        MetaTitle:                tour.MetaTitle,
        MetaDescription:          tour.MetaDescription,
        CreatedByUserId:          tour.CreatedByUserId,
        PlaceId:                  tour.PlaceId,
        CreatedAt:                tour.CreatedAt,
        UpdatedAt:                tour.UpdatedAt,
        SubmittedAt:              tour.SubmittedAt,
        ApprovedAt:               tour.ApprovedAt,
        ApprovedByUserId:         tour.ApprovedByUserId,
        RejectedAt:               tour.RejectedAt,
        RejectedByUserId:         tour.RejectedByUserId,
        RejectionReason:          tour.RejectionReason,
        SuspendedAt:              tour.SuspendedAt,
        SuspensionReason:         tour.SuspensionReason,
        ReinstatedAt:             tour.ReinstatedAt,
        Translations:             tour.TourTranslations
            .Select(t => new TourTranslationDto(t.LanguageId, t.Name, t.Description, t.ShortDescription, t.MeetingPoint))
            .ToList());
    }
}
