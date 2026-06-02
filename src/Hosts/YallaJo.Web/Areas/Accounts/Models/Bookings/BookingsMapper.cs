using YallaJo.Web.Areas.Accounts.Models.Bookings;
using YallaJo.Web.Areas.Accounts.Models.Bookings;

namespace YallaJo.Web.Areas.Accounts.Models.Bookings;

/// <summary>
/// Static, allocation-only projections from Booking DTOs to ViewModels.
/// Tour name / image hydration (which needs extra API calls) stays on the
/// facade; this mapper only composes already-resolved values.
/// </summary>
public static class BookingsMapper
{
    public static bool IsCancellable(string status) =>
        status is "AwaitingPayment" or "PendingConfirmation" or "Confirmed";

    public static BookingCardVm ToCardVm(MyBookingItemResponse item, string tourName, string? imageUrl) => new()
    {
        Id = item.Id,
        Reference = item.Reference,
        Status = item.Status,
        TourName = tourName,
        ImageUrl = imageUrl,
        ParticipantCount = item.ParticipantCount,
        TotalAmount = item.TotalAmount,
        Currency = item.Currency,
        Date = item.ConfirmedAt ?? item.CompletedAt ?? item.CancelledAt ?? item.CreatedAt,
        IsCancellable = IsCancellable(item.Status),
    };

    public static BookingDetailVm ToDetailVm(TourBookingDetailResponse d, string tourName, string? imageUrl)
    {
        var pricing = d.Pricing;
        return new BookingDetailVm
        {
            Id = d.Id,
            Reference = d.Reference,
            Status = d.Status,
            TourName = tourName,
            ImageUrl = imageUrl,
            ParticipantCount = d.ParticipantCount,
            IsInstantBooking = d.IsInstantBooking,
            SpecialRequests = d.SpecialRequests,
            CreatedAt = d.CreatedAt,
            Subtotal = pricing?.Subtotal ?? 0m,
            DiscountAmount = pricing?.DiscountAmount ?? 0m,
            LoyaltyAmount = pricing?.LoyaltyAmount ?? 0m,
            TotalAmount = pricing?.TotalAmount ?? 0m,
            Currency = pricing?.Currency ?? string.Empty,
            LineItems = pricing?.LineItems.Select(li => new BookingLineItemVm
            {
                TierType = li.TierType,
                Count = li.Count,
                UnitPrice = li.UnitPrice,
            }).ToList() ?? [],
            IsCancellable = IsCancellable(d.Status),
            CancellationReason = d.Cancellation?.Reason,
            CancelledAt = d.Cancellation?.CancelledAt,
            RefundAmount = d.Cancellation?.RefundAmount,
        };
    }
}
