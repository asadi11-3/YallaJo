using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Booking.Infrastructure.BackgroundServices.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booking.Infrastructure.Services;

internal sealed class SnapshotBookingCommissionLookup(
    ICommissionSnapshotRepository snapshotRepository,
    IOptions<BookingCommissionDefaultsOptions> options,
    ILogger<SnapshotBookingCommissionLookup> logger)
    : IBookingCommissionLookup
{
    public async Task<BookingCommissionLookupResult> GetForTourAsync(
        Guid tourId,
        CancellationToken cancellationToken = default)
    {
        var defaults = options.Value;
        var snapshot = await snapshotRepository
            .GetActiveByTierAsync(defaults.Tier, defaults.Currency, cancellationToken)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            logger.LogDebug(
                "No CommissionSnapshot found for Tier={Tier} Currency={Currency}; using fallback rate {Rate}.",
                defaults.Tier, defaults.Currency, defaults.FallbackRate);
            return new BookingCommissionLookupResult(defaults.FallbackRate);
        }

        // Finance stores Percentage as 0 < p < 100; Booking expects a fraction.
        var rate = snapshot.Percentage / 100m;
        logger.LogDebug(
            "CommissionSnapshot {RuleId} resolved Tier={Tier} Currency={Currency} -> Rate={Rate} (tourId hint {TourId}).",
            snapshot.Id, snapshot.Tier, snapshot.Currency, rate, tourId);
        return new BookingCommissionLookupResult(rate);
    }
}
