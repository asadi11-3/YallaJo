namespace YallaJo.Web.Areas.Guide.Models.Discounts;

public enum GuideDiscountType : byte
{
    Percentage = 0,
    FixedAmount = 1,
}

public sealed record GuideDiscountResponse(
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

public sealed record CreateGuideDiscountRequest(
    Guid? TourId,
    string Name,
    string? Description,
    GuideDiscountType DiscountType,
    decimal DiscountValue,
    string Currency,
    DateTime ValidFrom,
    DateTime? ValidUntil,
    int? MaxUsageCount);

public sealed record UpdateGuideDiscountRequest(
    string Name,
    string? Description,
    decimal DiscountValue,
    DateTime ValidFrom,
    DateTime? ValidUntil,
    int? MaxUsageCount);
