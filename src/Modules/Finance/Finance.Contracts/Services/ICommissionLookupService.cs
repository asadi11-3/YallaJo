namespace Finance.Contracts.Services;

/// <summary>
/// Cross-module read-only lookup of the commission rate that applies to a given tour.
/// Owned by Finance (the catalog of commission rules), consumed by Booking when pricing
/// a booking. Booking takes a dependency on <c>Finance.Contracts</c>, not on
/// <c>Finance.Infrastructure</c>, so the schema stays encapsulated.
/// </summary>
public interface ICommissionLookupService
{
    /// <summary>
    /// Returns the commission rule that applies to <paramref name="tourId"/>.
    /// Implementations MUST return a non-null result (use a default 0% rule if no
    /// override is configured).
    /// </summary>
    Task<CommissionResult> GetCommissionAsync(Guid tourId, CancellationToken ct = default);
}

/// <summary>
/// Commission lookup result. <see cref="Rate"/> is a fraction in [0, 1].
/// <see cref="MinimumAmount"/> and <see cref="MaximumAmount"/> are absolute caps
/// applied AFTER <c>Rate × bookingAmount</c>.
/// </summary>
public sealed record CommissionResult(decimal Rate, decimal MinimumAmount, decimal MaximumAmount);
