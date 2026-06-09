namespace YallaJo.Web.Areas.Admin.Models.Bookings;

public static class AdminBookingsMapper
{
    public static AdminBookingsIndexVm ToIndexVm(
        AdminBookingsPageResponse page,
        AdminBookingFiltersVm filters,
        IReadOnlyDictionary<Guid, string> tourNames) => new()
    {
        Filters    = filters,
        Items      = page.Items.Select(i => ToRowVm(i, Lookup(tourNames, i.TourId))).ToList(),
        NextCursor = page.NextCursor,
        TotalCount = page.TotalCount,
    };

    public static AdminBookingRowVm ToRowVm(AdminBookingItemResponse i, string tourName) => new()
    {
        Id               = i.Id,
        Reference        = i.Reference,
        Status           = i.Status,
        StatusBadgeClass = BadgeClass(i.Status),
        TourName         = tourName,
        ProviderId       = i.ProviderId,
        TravelerHandle   = ShortHandle(i.UserId),
        ParticipantCount = i.ParticipantCount,
        TotalAmount      = i.TotalAmount,
        Currency         = i.Currency,
        CreatedAt        = i.CreatedAt,
        RefundAmount     = i.RefundAmount,
    };

    public static AdminBookingDetailsVm ToDetailsVm(
        AdminBookingDetailResponse d,
        string tourName,
        IReadOnlyList<Payments.PaymentResponse>? payments = null) => new()
    {
        Id                 = d.Id,
        Reference          = d.Reference,
        Status             = d.Status,
        StatusBadgeClass   = BadgeClass(d.Status),
        TourName           = tourName,
        TourId             = d.TourId,
        ProviderId         = d.ProviderId,
        TravelerHandle     = ShortHandle(d.UserId),
        AvailabilitySlotId = d.AvailabilitySlotId,
        ParticipantCount   = d.ParticipantCount,
        IsInstantBooking   = d.IsInstantBooking,
        SpecialRequests    = d.SpecialRequests,
        CreatedAt          = d.CreatedAt,
        PaymentExpiresAt   = d.PaymentExpiresAt,
        UpdatedAt          = d.UpdatedAt,
        Subtotal           = d.Pricing?.Subtotal ?? 0m,
        DiscountAmount     = d.Pricing?.DiscountAmount ?? 0m,
        TotalAmount        = d.Pricing?.TotalAmount ?? 0m,
        Currency           = d.Pricing?.Currency ?? string.Empty,
        LineItems          = (d.Pricing?.LineItems ?? []).Select(l => new AdminBookingLineVm
        {
            TierType  = l.TierType,
            Count     = l.Count,
            UnitPrice = l.UnitPrice,
            Currency  = d.Pricing?.Currency ?? string.Empty,
        }).ToList(),
        IsCancelled        = d.Cancellation is not null,
        CancelledAt        = d.Cancellation?.CancelledAt,
        CancellationReason = d.Cancellation?.Reason,
        RefundAmount       = d.Cancellation?.RefundAmount,

        IsDisputed         = string.Equals(d.Status, "Disputed", StringComparison.OrdinalIgnoreCase),
        IsResolved         = string.Equals(d.Status, "Resolved", StringComparison.OrdinalIgnoreCase),
        DisputedAt         = d.Dispute?.DisputedAt,
        DisputeReason      = d.Dispute?.Reason,
        ResolvedAt         = d.Dispute?.ResolvedAt,
        ResolutionNotes    = d.Dispute?.ResolutionNotes,

        Payments           = (payments ?? []).Select(p => new PaymentOptionVm
        {
            Id            = p.Id,
            Amount        = p.Amount,
            Currency      = p.Currency,
            Status        = p.Status,
            PaymentType   = p.PaymentType,
            RefundedTotal = p.RefundedTotal,
            When          = p.PaidAt ?? p.CreatedAt,
        }).ToList(),
    };

    // ── Helpers ──────────────────────────────────────────────────────────────────────

    private static string Lookup(IReadOnlyDictionary<Guid, string> names, Guid tourId)
        => names.TryGetValue(tourId, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : $"Tour {tourId.ToString("N")[..8]}";

    /// <summary>Privacy-safe short traveler handle (no name/email).</summary>
    public static string ShortHandle(Guid userId) => $"Traveler {userId.ToString("N")[..8]}";

    public static string BadgeClass(string status) => status switch
    {
        "Confirmed"           => "bg-success",
        "Completed"           => "bg-primary",
        "AwaitingPayment"     => "bg-warning text-dark",
        "PendingConfirmation" => "bg-info text-dark",
        "Disputed"            => "bg-warning text-dark",
        "Resolved"            => "bg-primary",
        "Cancelled"           => "bg-secondary",
        "Rejected"            => "bg-danger",
        "Refunded"            => "bg-dark",
        "NoShow"              => "bg-danger",
        _                     => "bg-secondary",
    };

    /// <summary>A11Y5: a per-status Font Awesome icon so status is never colour-only.</summary>
    public static string StatusIcon(string status) => status switch
    {
        "Confirmed"           => "circle-check",
        "Completed"           => "flag-checkered",
        "AwaitingPayment"     => "hourglass-half",
        "PendingConfirmation" => "clock",
        "Disputed"            => "triangle-exclamation",
        "Resolved"            => "circle-check",
        "Cancelled"           => "ban",
        "Rejected"            => "circle-xmark",
        "Refunded"            => "rotate-left",
        "NoShow"              => "user-xmark",
        _                     => "circle-question",
    };
}
