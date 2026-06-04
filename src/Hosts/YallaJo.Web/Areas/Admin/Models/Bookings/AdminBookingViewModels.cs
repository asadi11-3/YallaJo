namespace YallaJo.Web.Areas.Admin.Models.Bookings;

/// <summary>Filter inputs for the admin booking dashboard.</summary>
public sealed class AdminBookingFiltersVm
{
    public string? Status { get; init; }
    public string? FromDate { get; init; }
    public string? ToDate { get; init; }
    public string? ProviderId { get; init; }
    public string? TourId { get; init; }
    public string? UserId { get; init; }

    public static readonly IReadOnlyList<string> StatusOptions =
        ["AwaitingPayment", "PendingConfirmation", "Confirmed", "Completed", "Cancelled", "Rejected", "Refunded", "NoShow"];
}

public sealed class AdminBookingsIndexVm
{
    public AdminBookingFiltersVm Filters { get; init; } = new();
    public IReadOnlyList<AdminBookingRowVm> Items { get; init; } = [];
    public string? NextCursor { get; init; }
    public int? TotalCount { get; init; }

    public bool HasItems => Items.Count > 0;
    public bool HasNextPage => !string.IsNullOrWhiteSpace(NextCursor);
}

public sealed class AdminBookingRowVm
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusBadgeClass { get; init; } = "bg-secondary";
    public string TourName { get; init; } = "Tour booking";
    public Guid ProviderId { get; init; }
    public string TravelerHandle { get; init; } = string.Empty;
    public int ParticipantCount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public decimal? RefundAmount { get; init; }
}

public sealed class AdminBookingDetailsVm
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusBadgeClass { get; init; } = "bg-secondary";
    public string TourName { get; init; } = "Tour booking";
    public Guid TourId { get; init; }
    public Guid ProviderId { get; init; }
    public string TravelerHandle { get; init; } = string.Empty;
    public Guid AvailabilitySlotId { get; init; }
    public int ParticipantCount { get; init; }
    public bool IsInstantBooking { get; init; }
    public string? SpecialRequests { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? PaymentExpiresAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public IReadOnlyList<AdminBookingLineVm> LineItems { get; init; } = [];

    public bool IsCancelled { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    public decimal? RefundAmount { get; init; }
}

public sealed class AdminBookingLineVm
{
    public string TierType { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal UnitPrice { get; init; }
    public string Currency { get; init; } = string.Empty;
}
