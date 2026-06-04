using System.Globalization;

namespace YallaJo.Web.Areas.Provider.Models.Bookings;

public static class ProviderBookingsMapper
{
    public static ProviderBookingsIndexVm ToIndexVm(
        ProviderBookingsPageResponse page,
        string? status,
        IReadOnlyDictionary<Guid, string> tourNames) => new()
    {
        Status = status,
        Items = page.Items.Select(i => ToRowVm(i, Lookup(tourNames, i.TourId))).ToList(),
    };

    public static ProviderBookingRowVm ToRowVm(ProviderBookingItemResponse i, string tourName) => new()
    {
        Id               = i.Id,
        Reference        = i.Reference,
        Status           = i.Status,
        StatusBadgeClass = BadgeClass(i.Status),
        TourName         = tourName,
        TravelerHandle   = ShortHandle(i.UserId),
        SlotLabel        = SlotLabel(i.SlotDate, i.SlotStartTime, i.SlotEndTime),
        ParticipantCount = i.ParticipantCount,
        TotalAmount      = i.TotalAmount,
        Currency         = i.Currency,
        CreatedAt        = i.CreatedAt,
    };

    public static ProviderBookingDetailsVm ToDetailsVm(
        ProviderBookingDetailResponse d, string tourName, string slotLabel) => new()
    {
        Id                 = d.Id,
        Reference          = d.Reference,
        Status             = d.Status,
        StatusBadgeClass   = BadgeClass(d.Status),
        TourName           = tourName,
        TravelerHandle     = ShortHandle(d.UserId),
        SlotLabel          = slotLabel,
        ParticipantCount   = d.ParticipantCount,
        IsInstantBooking   = d.IsInstantBooking,
        SpecialRequests    = d.SpecialRequests,
        CreatedAt          = d.CreatedAt,
        Subtotal           = d.Pricing?.Subtotal ?? 0m,
        DiscountAmount     = d.Pricing?.DiscountAmount ?? 0m,
        TotalAmount        = d.Pricing?.TotalAmount ?? 0m,
        Currency           = d.Pricing?.Currency ?? string.Empty,
        LineItems          = (d.Pricing?.LineItems ?? []).Select(l => new ProviderBookingLineVm
        {
            TierType  = l.TierType,
            Count     = l.Count,
            UnitPrice = l.UnitPrice,
            Currency  = d.Pricing?.Currency ?? string.Empty,
        }).ToList(),
        IsCancelled        = d.Cancellation is not null,
        CancellationReason = d.Cancellation?.Reason,
        RefundAmount       = d.Cancellation?.RefundAmount,
    };

    // ── Helpers ──────────────────────────────────────────────────────────────────────

    private static string Lookup(IReadOnlyDictionary<Guid, string> names, Guid tourId)
        => names.TryGetValue(tourId, out var name) && !string.IsNullOrWhiteSpace(name) ? name : "Tour booking";

    /// <summary>Privacy-safe short traveler handle (no name/email). E.g. "Traveler b0000000".</summary>
    public static string ShortHandle(Guid userId)
        => $"Traveler {userId.ToString("N")[..8]}";

    public static string SlotLabel(DateOnly? date, TimeOnly? start, TimeOnly? end)
    {
        if (date is null) return "—";
        var d = date.Value.ToString("ddd, dd MMM yyyy", CultureInfo.InvariantCulture);
        if (start is null) return d;
        var time = end is null
            ? start.Value.ToString("HH:mm", CultureInfo.InvariantCulture)
            : $"{start.Value:HH\\:mm} – {end.Value:HH\\:mm}";
        return $"{d} · {time}";
    }

    public static string BadgeClass(string status) => status switch
    {
        "Confirmed"          => "bg-success",
        "Completed"          => "bg-primary",
        "AwaitingPayment"    => "bg-warning text-dark",
        "PendingConfirmation"=> "bg-info text-dark",
        "Cancelled"          => "bg-secondary",
        "Rejected"           => "bg-danger",
        "Refunded"           => "bg-dark",
        "NoShow"             => "bg-danger",
        _                     => "bg-secondary",
    };
}
