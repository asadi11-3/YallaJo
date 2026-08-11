using Booking.Contracts.Authorization;
using Tracking.Application.Common;
using Tracking.Domain.Entities;
using Tracking.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Tracking.Application.Queries.GetTrackingHistory;

public sealed class GetTrackingHistoryQueryHandler(
    ITrackingSessionRepository trackingSessionRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetTrackingHistoryQuery, TrackingHistoryPage>
{
    public async Task<Result<TrackingHistoryPage>> Handle(
        GetTrackingHistoryQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = currentUser.UserId;
            if (!currentUser.IsAuthenticated || actorUserId is null)
            {
                return Result.Failure<TrackingHistoryPage>(
                    new Error("Tracking.Unauthorized", "Authentication is required."),
                    Outcome.Unauthorized);
            }

            if (request.UserId != actorUserId.Value && !TrackingAuthorization.IsAdmin(currentUser))
            {
                return Result.Failure<TrackingHistoryPage>(
                    new Error("Tracking.Forbidden", "You are not allowed to view this tracking history."),
                    Outcome.Forbidden);
            }

            if (!TrackingHistoryCursor.TryDecode(request.Cursor, out var cursor))
            {
                return Result.Failure<TrackingHistoryPage>(
                    new Error("Tracking.InvalidCursor", "The pagination cursor is malformed."),
                    Outcome.Invalid);
            }

            var pageSize = request.EffectivePageSize;
            var limit = pageSize + 1;

            var rows = await trackingSessionRepository
                .GetSessionHistoryByUserIdAsync(
                    request.UserId,
                    request.Statuses,
                    cursor?.StartedAt,
                    cursor?.Id,
                    limit,
                    cancellationToken)
                .ConfigureAwait(false);

            string? nextCursor = null;
            IReadOnlyList<LiveTrackingSession> pageRows = rows;

            if (rows.Count > pageSize)
            {
                var trimmed = new LiveTrackingSession[pageSize];
                for (var i = 0; i < pageSize; i++)
                {
                    trimmed[i] = rows[i];
                }

                pageRows = trimmed;
                var last = trimmed[pageSize - 1];
                nextCursor = new TrackingHistoryCursor(last.Id, last.StartedAt).Encode();
            }

            return Result.Success(new TrackingHistoryPage(
                pageRows.Select(x => x.ToSummary()).ToArray(),
                nextCursor));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<TrackingHistoryPage>(
                new Error("Request.Cancelled", "Operation was cancelled."),
                Outcome.Canceled);
        }
    }
}
