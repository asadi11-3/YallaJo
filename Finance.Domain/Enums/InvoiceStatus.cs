namespace Finance.Domain.Enums;

/// <summary>
/// Lifecycle states for an <see cref="Entities.Invoice"/>.
/// </summary>
public enum InvoiceStatus : byte
{
    /// <summary>
    /// Invoice has been generated and is the authoritative billing record for a completed payment.
    /// </summary>
    Issued = 0,

    /// <summary>
    /// Invoice has been administratively voided; supersedes the issued state but preserves history.
    /// </summary>
    Cancelled = 1,

    /// <summary>
    /// Invoice corresponds to a payment that has been (fully or partially) refunded.
    /// </summary>
    Refunded = 2,
}
