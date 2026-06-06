namespace YallaJo.Web.Areas.Admin.Models.Bookings;

/// <summary>
/// Body for POST /api/v1/booking/admin/{id}/dispute/resolve (FE-1A-2). Admin-only.
/// Backend validates ResolutionNotes is 10-2000 chars and the booking is Disputed.
/// </summary>
public sealed record ResolveBookingDisputeRequest(string ResolutionNotes);

/// <summary>
/// Body for POST /api/v1/payments/{id}/refund (FE-1A-3). Issued only from inside the
/// resolve-dispute modal. Amount is in the booking currency.
/// </summary>
public sealed record RefundPaymentRequest(decimal Amount, string Currency, string Reason);

/// <summary>
/// Facade-level refund intent attached to a resolve action (FE-1A-3). Carries the
/// PaymentId the admin supplied in the resolve-dispute modal's optional refund section.
/// </summary>
public sealed record RefundRequest(Guid PaymentId, decimal Amount, string Currency, string Reason);

/// <summary>
/// Result of the resolve(+optional refund) facade operation. Distinguishes the four
/// outcomes the UI must message differently, including the partial-failure case where
/// the dispute resolved but the refund did not.
/// </summary>
public sealed class ResolveDisputeOutcome
{
    public enum OutcomeKind
    {
        ResolvedNoRefund,
        ResolvedAndRefunded,
        ResolvedButRefundFailed,
        ResolveFailed,
        SignOut,
    }

    public OutcomeKind Kind { get; private init; }
    public string? Message { get; private init; }

    public bool IsResolved => Kind is OutcomeKind.ResolvedNoRefund
        or OutcomeKind.ResolvedAndRefunded
        or OutcomeKind.ResolvedButRefundFailed;

    public bool RequireSignOut => Kind == OutcomeKind.SignOut;

    public static ResolveDisputeOutcome ResolvedNoRefund() =>
        new() { Kind = OutcomeKind.ResolvedNoRefund };

    public static ResolveDisputeOutcome ResolvedAndRefunded() =>
        new() { Kind = OutcomeKind.ResolvedAndRefunded };

    public static ResolveDisputeOutcome ResolvedButRefundFailed(string refundError) =>
        new() { Kind = OutcomeKind.ResolvedButRefundFailed, Message = refundError };

    public static ResolveDisputeOutcome ResolveFailed(string error) =>
        new() { Kind = OutcomeKind.ResolveFailed, Message = error };

    public static ResolveDisputeOutcome SignOut() =>
        new() { Kind = OutcomeKind.SignOut };
}
