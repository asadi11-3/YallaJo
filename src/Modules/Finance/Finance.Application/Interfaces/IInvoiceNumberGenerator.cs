namespace Finance.Application.Interfaces;

/// <summary>
/// Allocates the next sequential invoice number in the form <c>INV-{YYYYMM}-{seq6}</c>.
/// Implementations must guarantee race-free monotonicity (e.g. via SQL row-lock UPSERT).
/// Resets per calendar month (UTC).
/// </summary>
public interface IInvoiceNumberGenerator
{
    /// <summary>
    /// Reserves and returns the next invoice number for the current UTC month.
    /// </summary>
    Task<string> NextAsync(CancellationToken ct = default);
}
