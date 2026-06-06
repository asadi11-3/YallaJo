using ContentTours.Application.Interfaces;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.Snapshot.TriggerTourSnapshotBackfill;

public sealed class TriggerTourSnapshotBackfillCommandHandler(
    ITourSnapshotBackfillService backfillService,
    ILogger<TriggerTourSnapshotBackfillCommandHandler> logger)
    : ICommandHandler<TriggerTourSnapshotBackfillCommand, TriggerTourSnapshotBackfillResult>
{
    public async Task<Result<TriggerTourSnapshotBackfillResult>> Handle(
        TriggerTourSnapshotBackfillCommand request,
        CancellationToken cancellationToken)
    {
        var batchSize = request.BatchSize <= 0 ? 100 : request.BatchSize;

        logger.LogInformation(
            "Tour snapshot backfill starting (batchSize={BatchSize})", batchSize);

        try
        {
            var enqueued = await backfillService
                .BackfillApprovedToursAsync(batchSize, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Tour snapshot backfill complete: {ToursEnqueued} approved tour(s) enqueued", enqueued);

            return Result.Success(new TriggerTourSnapshotBackfillResult(enqueued));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<TriggerTourSnapshotBackfillResult>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
