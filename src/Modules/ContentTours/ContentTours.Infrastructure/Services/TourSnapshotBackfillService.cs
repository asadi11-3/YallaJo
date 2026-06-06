using ContentTours.Application.Interfaces;
using ContentTours.Contracts;
using ContentTours.Domain.Enums;
using ContentTours.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.Services;

/// <summary>
/// One-time backfill that re-emits the enriched <see cref="TourApprovedIntegrationEvent"/>
/// for every already-approved tour, so the Booking module's TourSnapshot rows are
/// repopulated with real Title/BasePrice/Currency/IsInstantBooking/MaxGroupSize values.
/// Pre-existing tours approved before the event chain was enriched otherwise carry
/// stale/zero snapshot data.
/// </summary>
internal sealed class TourSnapshotBackfillService(
    ContentToursDbContext dbContext,
    ILogger<TourSnapshotBackfillService> logger) : ITourSnapshotBackfillService
{
    public async Task<int> BackfillApprovedToursAsync(int batchSize, CancellationToken ct = default)
    {
        if (batchSize <= 0)
        {
            batchSize = 100;
        }

        var totalEnqueued = 0;
        var skip = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var batch = await dbContext.Tours
                .AsNoTracking()
                .Where(t => t.Status == TourStatus.Approved)
                .OrderBy(t => t.Id)
                .Skip(skip)
                .Take(batchSize)
                .ToListAsync(ct);

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var tour in batch)
            {
                dbContext.OutboxMessages.Add(OutboxMessage.Create(
                    new TourApprovedIntegrationEvent(
                        TourId: tour.Id,
                        CreatedByUserId: tour.CreatedByUserId,
                        ApprovedByUserId: tour.CreatedByUserId,
                        ApprovedAt: DateTime.UtcNow,
                        Title: tour.Name,
                        BasePrice: tour.BasePrice.Amount,
                        Currency: tour.Currency,
                        IsInstantBooking: tour.IsInstantBooking,
                        MaxGroupSize: tour.MaxGroupSize)));
            }

            await dbContext.SaveChangesAsync(ct);

            totalEnqueued += batch.Count;
            skip += batch.Count;

            logger.LogInformation(
                "Tour snapshot backfill: enqueued {BatchCount} events (running total {TotalEnqueued}).",
                batch.Count,
                totalEnqueued);

            if (batch.Count < batchSize)
            {
                break;
            }
        }

        logger.LogInformation(
            "Tour snapshot backfill complete: {TotalEnqueued} TourApproved events enqueued.",
            totalEnqueued);

        return totalEnqueued;
    }
}
