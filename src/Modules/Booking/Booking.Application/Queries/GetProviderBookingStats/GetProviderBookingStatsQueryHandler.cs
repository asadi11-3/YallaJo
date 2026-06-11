using Booking.Application.Interfaces;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetProviderBookingStats;

/// <summary>
/// [Backend] B5: resolves the caller's provider and returns grouped per-status
/// booking counts in a single DB query (API7 — aggregate over N round trips).
/// </summary>
public sealed class GetProviderBookingStatsQueryHandler(
    ITourBookingRepository repository,
    IBookingProviderSnapshotReader providerSnapshotReader,
    ILogger<GetProviderBookingStatsQueryHandler> logger)
    : IQueryHandler<GetProviderBookingStatsQuery, ProviderBookingStatsDto>
{
    public async Task<Result<ProviderBookingStatsDto>> Handle(
        GetProviderBookingStatsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.FromDate is { } from && request.ToDate is { } to && from > to)
        {
            return Result.Failure<ProviderBookingStatsDto>(
                new Error("TourBooking.InvalidDateRange", "fromDate must be on or before toDate."),
                Outcome.Invalid);
        }

        // No provider → zero stats (never another provider's data).
        var provider = await providerSnapshotReader
            .GetByOwnerUserIdAsync(request.CallerUserId, cancellationToken)
            .ConfigureAwait(false);

        if (provider is null)
        {
            logger.LogInformation(
                "GetProviderBookingStats: caller {UserId} owns no provider; returning zero stats.",
                request.CallerUserId);
            return Result.Success(new ProviderBookingStatsDto(0, 0, 0, 0, 0, 0));
        }

        var counts = await repository.GetProviderBookingStatusCountsAsync(
            provider.ProviderId,
            request.FromDate,
            request.ToDate,
            cancellationToken).ConfigureAwait(false);

        int Of(BookingStatus s) => counts.TryGetValue(s, out var c) ? c : 0;

        var dto = new ProviderBookingStatsDto(
            Total: counts.Values.Sum(),
            Pending: Of(BookingStatus.AwaitingPayment) + Of(BookingStatus.PendingConfirmation),
            Confirmed: Of(BookingStatus.Confirmed),
            Completed: Of(BookingStatus.Completed),
            Cancelled: Of(BookingStatus.Cancelled),
            Rejected: Of(BookingStatus.Rejected));

        return Result.Success(dto);
    }
}
