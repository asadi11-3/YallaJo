using YallaJo.SharedKernel.Domain.Entities;

namespace Booking.Domain.Entities;

/// <summary>
/// Refund tier policy attached to tours/providers. The booking captures a JSON snapshot of this
/// policy at creation time so subsequent edits don't retroactively affect outstanding bookings.
/// </summary>
public sealed class RefundPolicy : AuditableEntity, IAggregateRoot
{
    private RefundPolicy() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>Hours before tour start where a full (100%) refund still applies.</summary>
    public int FullRefundHours { get; private set; }

    /// <summary>Hours before tour start where a partial refund (<see cref="PartialRefundPercent"/>) applies.</summary>
    public int PartialRefundHours { get; private set; }

    /// <summary>Partial refund percent (0-100) used between <see cref="PartialRefundHours"/> and <see cref="FullRefundHours"/>.</summary>
    public decimal PartialRefundPercent { get; private set; }

    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Computes the applicable refund percent (0-100) for a USER-initiated cancellation.
    /// Provider-initiated and force-majeure cancels bypass this and pay 100% directly.
    /// </summary>
    /// <param name="timeUntilTour">Time remaining until the tour's slot start.</param>
    /// <returns>Refund percentage in [0, 100].</returns>
    public decimal CalculateRefundPercentage(TimeSpan timeUntilTour)
    {
        var hoursRemaining = timeUntilTour.TotalHours;
        if (hoursRemaining >= FullRefundHours) return 100m;
        if (hoursRemaining >= PartialRefundHours) return Math.Clamp(PartialRefundPercent, 0m, 100m);
        return 0m;
    }
}
