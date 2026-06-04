namespace YallaJo.Web.Areas.Provider.Models.Bookings;

public sealed class BookingsVm
{
    public IReadOnlyList<JoinRequestRowVm> JoinRequests { get; init; } = [];
    public BookingDetailVm? LookupResult { get; init; }
    public Guid? LookupId { get; init; }
    public string? LookupError { get; init; }

    public bool HasJoinRequests => JoinRequests.Count > 0;
    public int PendingCount => JoinRequests.Count(r => r.IsPending);
}

public sealed class JoinRequestRowVm
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public int ParticipantCount { get; init; }
    public string? Message { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string? ResponseMessage { get; init; }

    public bool IsPending => string.Equals(Status, "Pending", StringComparison.OrdinalIgnoreCase);
    public bool IsExpired => ExpiresAt <= DateTime.UtcNow && IsPending;
}

public sealed class BookingDetailVm
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int ParticipantCount { get; init; }
    public bool IsInstantBooking { get; init; }
    public string? SpecialRequests { get; init; }
    public DateTime CreatedAt { get; init; }

    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal LoyaltyAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public IReadOnlyList<BookingLineVm> LineItems { get; init; } = [];

    public string? CancelledReason { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? RejectionReason { get; init; }

    public bool IsConfirmable =>
        string.Equals(Status, "PendingConfirmation", StringComparison.OrdinalIgnoreCase);

    public bool IsRejectable =>
        string.Equals(Status, "PendingConfirmation", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Status, "AwaitingPayment", StringComparison.OrdinalIgnoreCase);
}

public sealed class BookingLineVm
{
    public string? TierType { get; init; }
    public int Count { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal => UnitPrice * Count;
}
