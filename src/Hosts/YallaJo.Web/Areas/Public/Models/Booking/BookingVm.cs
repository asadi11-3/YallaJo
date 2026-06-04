using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Public.Models.Booking;

/// <summary>A single bookable date+slot offered to the customer.</summary>
public sealed class BookingSlotVm
{
    public Guid Id { get; init; }
    public DateOnly Date { get; init; }
    public string? StartTime { get; init; }
    public string? EndTime { get; init; }
    public int AvailableCount { get; init; }

    /// <summary>The guide assigned to this slot — supplied to the booking create request.</summary>
    public Guid TourGuideId { get; init; }
}

/// <summary>The tour-booking page (step 1+2): tour summary, available slots, traveler form.</summary>
public sealed class TourBookingVm
{
    public Guid TourId { get; init; }
    public string TourName { get; init; } = string.Empty;
    public string? TourSlug { get; init; }
    public string? ImageUrl { get; init; }
    public string? ShortDescription { get; init; }
    public string? Difficulty { get; init; }
    public int DurationMinutes { get; init; }
    public int? MaxGroupSize { get; init; }
    public decimal BasePrice { get; init; }
    public decimal? SalePrice { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public bool IsInstantBooking { get; init; }
    public int? CancellationPolicyHours { get; init; }

    public IReadOnlyList<BookingSlotVm> Slots { get; init; } = [];

    /// <summary>The bound form (re-populated on validation errors).</summary>
    public TourBookingFormVm Form { get; set; } = new();

    public decimal EffectivePrice => SalePrice is { } s && s > 0 && s < BasePrice ? s : BasePrice;
    public bool HasDiscount => SalePrice is { } s && s > 0 && s < BasePrice;
    public int DiscountPercent => HasDiscount && BasePrice > 0
        ? (int)Math.Round((BasePrice - SalePrice!.Value) / BasePrice * 100m)
        : 0;
    public bool HasSlots => Slots.Count > 0;

    /// <summary>
    /// Whether at least one slot exposes a guide. Booking requires a guide, so the
    /// form is disabled with a friendly message when no guide is assigned.
    /// </summary>
    public bool HasGuide => Slots.Any(s => s.TourGuideId != Guid.Empty);

    /// <summary>Largest seat count across all slots — caps the participant inputs client-side.</summary>
    public int MaxAvailableCount => HasSlots ? Slots.Max(s => s.AvailableCount) : 0;

    /// <summary>
    /// Client-side ESTIMATE only (effective per-person price × total participants).
    /// The server recalculates the authoritative total (per-tier pricing, discounts,
    /// commission) on submit; this is purely indicative.
    /// </summary>
    public decimal EstimatedTotal
    {
        get
        {
            var participants = Math.Max(1, Form.Adults + Form.Children + Form.Infants + Form.Seniors);
            return EffectivePrice * participants;
        }
    }

    public string DurationLabel
    {
        get
        {
            if (DurationMinutes <= 0) return "Flexible";
            var h = DurationMinutes / 60;
            var m = DurationMinutes % 60;
            if (h > 0 && m > 0) return $"{h}h {m}m";
            return h > 0 ? $"{h}h" : $"{m}m";
        }
    }
}

/// <summary>Posted form when a (signed-in) customer confirms a booking.</summary>
public sealed class TourBookingFormVm
{
    [Required]
    public Guid TourId { get; set; }

    [Required(ErrorMessage = "Please choose an available date/time.")]
    public Guid AvailabilitySlotId { get; set; }

    /// <summary>
    /// The guide for the chosen slot. Set client-side from the selected slot and
    /// re-validated server-side against the slot before the API call.
    /// </summary>
    public Guid GuideId { get; set; }

    [Range(1, 50, ErrorMessage = "At least one adult is required.")]
    [Display(Name = "Adults")]
    public int Adults { get; set; } = 1;

    [Range(0, 50)]
    [Display(Name = "Children")]
    public int Children { get; set; }

    [Range(0, 50)]
    [Display(Name = "Infants")]
    public int Infants { get; set; }

    [Range(0, 50)]
    [Display(Name = "Seniors")]
    public int Seniors { get; set; }

    [Display(Name = "Private tour")]
    public bool IsPrivate { get; set; }

    [StringLength(50)]
    [Display(Name = "Promo code")]
    public string? PromoCode { get; set; }

    [StringLength(1000)]
    [Display(Name = "Special requests")]
    public string? SpecialRequests { get; set; }
}

/// <summary>The booking-confirmation page after a successful create.</summary>
public sealed class BookingConfirmVm
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string TourName { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public int ParticipantCount { get; init; }
    public bool IsInstantBooking { get; init; }
    public string? SpecialRequests { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? PaymentExpiresAt { get; init; }

    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal LoyaltyAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public IReadOnlyList<BookingConfirmLineVm> LineItems { get; init; } = [];

    public bool IsCancelled { get; init; }
    public DateTime? CancelledAt { get; init; }
    public decimal? RefundAmount { get; init; }

    public bool AwaitingPayment =>
        string.Equals(Status, "AwaitingPayment", StringComparison.OrdinalIgnoreCase);
}

public sealed class BookingConfirmLineVm
{
    public string? TierType { get; init; }
    public int Count { get; init; }
    public decimal UnitPrice { get; init; }
}
