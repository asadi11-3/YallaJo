using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.Snapshot.TriggerTourSnapshotBackfill;

/// <summary>
/// One-time ops command that re-emits the enriched TourApproved integration event
/// for every already-approved tour, so the Booking module can backfill its
/// TourSnapshots table with real values (price/title/instant/capacity).
/// </summary>
/// <param name="BatchSize">How many tours to process per SaveChanges batch (default 100).</param>
public sealed record TriggerTourSnapshotBackfillCommand(int BatchSize = 100)
    : ICommand<TriggerTourSnapshotBackfillResult>;

/// <summary>Result of the backfill: number of approved tours enqueued into the outbox.</summary>
public sealed record TriggerTourSnapshotBackfillResult(int ToursEnqueued);
