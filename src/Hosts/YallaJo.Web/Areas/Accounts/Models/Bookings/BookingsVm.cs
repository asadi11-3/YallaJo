using YallaJo.Web.Areas.Accounts.Models.JoinRequests;

namespace YallaJo.Web.Areas.Accounts.Models.Bookings;

public sealed class BookingsVm
{
    public string ActiveTab { get; init; } = "Upcoming";
    public IReadOnlyList<BookingCardVm> Bookings { get; init; } = [];
    public IReadOnlyList<string> Tabs { get; init; } = [];

    /// <summary>
    /// Phase 3 (Accounts plan): join requests rendered as an extra tab on My Trips.
    /// Populated only when ActiveTab == "join-requests".
    /// </summary>
    public MyJoinRequestsVm? JoinRequests { get; set; }
}

public sealed class BookingCardVm
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string TourName { get; init; } = "Tour booking";
    public string? ImageUrl { get; init; }
    public int ParticipantCount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public bool IsCancellable { get; init; }
}

public sealed class BookingDetailVm
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string TourName { get; init; } = "Tour booking";
    public string? ImageUrl { get; init; }
    public int ParticipantCount { get; init; }
    public bool IsInstantBooking { get; init; }
    public string? SpecialRequests { get; init; }
    public DateTime CreatedAt { get; init; }

    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal LoyaltyAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public IReadOnlyList<BookingLineItemVm> LineItems { get; init; } = [];

    public bool IsCancellable { get; init; }
    public string? CancellationReason { get; init; }
    public DateTime? CancelledAt { get; init; }
    public decimal? RefundAmount { get; init; }

    // ── Dispute lifecycle (FE-1A) ──────────────────────────────────────────────
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// True when the owner may open a dispute: booking is Completed AND completed
    /// within the past 48 hours AND not already disputed. Defensive only — the
    /// backend re-enforces both the owner check and the window.
    /// </summary>
    public bool IsDisputable { get; init; }

    public bool IsDisputed { get; init; }
    public bool IsResolved { get; init; }
    public DateTime? DisputedAt { get; init; }
    public string? DisputeReason { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? ResolutionNotes { get; init; }
}

public sealed class BookingLineItemVm
{
    public string TierType { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal UnitPrice { get; init; }
}
