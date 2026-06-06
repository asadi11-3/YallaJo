namespace ContentTours.Application.Interfaces;

/// <summary>
/// One-time backfill port: re-emits the (now enriched) TourApproved integration event
/// into the outbox for every already-approved tour, so downstream modules (Booking)
/// can (re)populate their owned snapshot tables with real Title/BasePrice/Currency/
/// IsInstantBooking/MaxGroupSize values.
/// Implemented in the Infrastructure layer (which owns the DbContext + outbox).
/// </summary>
public interface ITourSnapshotBackfillService
{
    /// <summary>
    /// Enumerates approved tours in batches and enqueues an enriched TourApproved
    /// integration event per tour into the outbox. Idempotent at the consumer side
    /// (the Booking snapshot handler upserts). Returns the number of tours enqueued.
    /// </summary>
    Task<int> BackfillApprovedToursAsync(int batchSize, CancellationToken ct = default);
}
