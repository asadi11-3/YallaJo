using YallaJo.Web.Areas.Accounts.Models.Payments;

namespace YallaJo.Web.Areas.Accounts.Models.Disputes;

/// <summary>
/// Pure projections for the §3.8 Disputes page. No I/O / HttpContext.
/// </summary>
public static class DisputesMapper
{
    public static DisputeRowVm ToRow(DisputeResponse d) => new(
        d.Id,
        d.PaymentId,
        d.Reason,
        d.Description,
        string.IsNullOrWhiteSpace(d.Status) ? "Open" : d.Status!,
        d.Resolution,
        d.ResolutionNotes,
        d.CreatedAt,
        d.ResolvedAt);

    /// <summary>
    /// A payment is offered as a dispute target only when it has actually been
    /// charged (a paid/refunded payment) — pending/failed payments cannot be disputed.
    /// The backend remains the source of truth and re-validates on POST.
    /// </summary>
    public static DisputablePaymentVm ToDisputable(PaymentRowVm p) => new(
        p.Id,
        p.BookingId,
        p.Amount,
        p.Currency,
        p.CreatedAt);

    public static bool IsDisputable(PaymentRowVm p) =>
        p.Status is "Succeeded" or "Paid" or "Refunded" or "PartiallyRefunded"
        || p.PaidAt is not null;
}
