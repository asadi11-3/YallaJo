using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Provider.Models.Bookings;

public sealed class ProviderBookingsIndexVm
{
    public string? Status { get; init; }
    public IReadOnlyList<ProviderBookingRowVm> Items { get; init; } = [];
    public bool HasItems => Items.Count > 0;

    /// <summary>Status values offered in the filter dropdown.</summary>
    public static readonly IReadOnlyList<string> StatusFilters =
        ["AwaitingPayment", "PendingConfirmation", "Confirmed", "Completed", "Cancelled", "Rejected"];
}

public sealed class ProviderBookingRowVm
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusBadgeClass { get; init; } = "bg-secondary";
    public string TourName { get; init; } = "Tour booking";
    public string TravelerHandle { get; init; } = string.Empty;
    public string SlotLabel { get; init; } = "—";
    public int ParticipantCount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class ProviderBookingDetailsVm
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusBadgeClass { get; init; } = "bg-secondary";
    public string TourName { get; init; } = "Tour booking";
    public string TravelerHandle { get; init; } = string.Empty;
    public string SlotLabel { get; init; } = "—";
    public int ParticipantCount { get; init; }
    public bool IsInstantBooking { get; init; }
    public string? SpecialRequests { get; init; }
    public DateTime CreatedAt { get; init; }

    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public IReadOnlyList<ProviderBookingLineVm> LineItems { get; init; } = [];

    public bool IsCancelled { get; init; }
    public string? CancellationReason { get; init; }
    public decimal? RefundAmount { get; init; }

    // Action flags mirror the backend state machine for the actions in this batch:
    //   Confirm ← AwaitingPayment | PendingConfirmation
    //   Cancel  ← AwaitingPayment | PendingConfirmation | Confirmed
    public bool CanConfirm => Status is "AwaitingPayment" or "PendingConfirmation";
    public bool CanReject  => Status is "PendingConfirmation";
    public bool CanCancel  => Status is "AwaitingPayment" or "PendingConfirmation" or "Confirmed";
    public bool HasAnyAction => CanConfirm || CanReject || CanCancel;
}

public sealed class ProviderBookingLineVm
{
    public string TierType { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal UnitPrice { get; init; }
    public string Currency { get; init; } = string.Empty;
}

/// <summary>Bound from the cancel form on the details page.</summary>
public sealed class ProviderBookingCancelVm
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "A cancellation reason is required.")]
    [MinLength(10, ErrorMessage = "Please provide a reason of at least 10 characters.")]
    [MaxLength(500)]
    [Display(Name = "Cancellation reason")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Bound from the reject form on the details page (PendingConfirmation only).</summary>
public sealed class ProviderBookingRejectVm
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "A rejection reason is required.")]
    [MinLength(10, ErrorMessage = "Please provide a reason of at least 10 characters.")]
    [MaxLength(500)]
    [Display(Name = "Rejection reason")]
    public string Reason { get; set; } = string.Empty;
}
