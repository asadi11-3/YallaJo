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

    /// <summary>
    /// FE-1A: an owner may open a dispute when the booking is Completed and the
    /// completion happened within the past 48 hours, and no dispute is open yet.
    /// Authoritative enforcement is server-side; this only governs button visibility.
    /// </summary>
    public static bool IsDisputable(string status, DateTime? completedAt, bool alreadyDisputed) =>
        string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase)
        && completedAt is { } c
        && DateTime.UtcNow <= c.AddHours(48)
        && !alreadyDisputed;

    public static BookingDetailVm ToDetailVm(TourBookingDetailResponse d, string tourName, string? imageUrl)
    {
        var pricing = d.Pricing;
        var dispute = d.Dispute;
        var completedAt = d.Completion?.CompletedAt;
        var isDisputed = string.Equals(d.Status, "Disputed", StringComparison.OrdinalIgnoreCase);
        var isResolved = string.Equals(d.Status, "Resolved", StringComparison.OrdinalIgnoreCase);

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

            // ── Dispute lifecycle (FE-1A) ──────────────────────────────────────
            CompletedAt = completedAt,
            IsDisputable = IsDisputable(d.Status, completedAt, alreadyDisputed: dispute is not null || isDisputed),
            IsDisputed = isDisputed,
            IsResolved = isResolved,
            DisputedAt = dispute?.DisputedAt,
            DisputeReason = dispute?.Reason,
            ResolvedAt = dispute?.ResolvedAt,
            ResolutionNotes = dispute?.ResolutionNotes,
        };
    }
}
