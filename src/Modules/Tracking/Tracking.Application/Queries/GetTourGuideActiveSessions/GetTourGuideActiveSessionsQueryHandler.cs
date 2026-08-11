using Booking.Contracts.Authorization;
using Tracking.Application.Common;
using Tracking.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Tracking.Application.Queries.GetTourGuideActiveSessions;

public sealed class GetTourGuideActiveSessionsQueryHandler(
    ITrackingSessionRepository trackingSessionRepository,
    ITourGuideOwnershipService tourGuideOwnershipService,
    ICurrentUser currentUser)
    : IQueryHandler<GetTourGuideActiveSessionsQuery, IReadOnlyList<TrackingSessionSummaryDto>>
{
    public async Task<Result<IReadOnlyList<TrackingSessionSummaryDto>>> Handle(
        GetTourGuideActiveSessionsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = currentUser.UserId;
            if (!currentUser.IsAuthenticated || actorUserId is null)
            {
                return Result.Failure<IReadOnlyList<TrackingSessionSummaryDto>>(
                    new Error("Tracking.Unauthorized", "Authentication is required."),
                    Outcome.Unauthorized);
            }

            if (!TrackingAuthorization.IsAdmin(currentUser))
            {
                var canManageGuide = await TrackingAuthorization
                    .CanManageGuideAsync(request.TourGuideId, actorUserId.Value, tourGuideOwnershipService, cancellationToken)
                    .ConfigureAwait(false);

                if (!canManageGuide)
                {
                    return Result.Failure<IReadOnlyList<TrackingSessionSummaryDto>>(
                        new Error("Tracking.Forbidden", "You are not allowed to view tracking sessions for this guide."),
                        Outcome.Forbidden);
                }
            }

            var sessions = await trackingSessionRepository
                .GetActiveSessionsByGuideIdAsync(request.TourGuideId, cancellationToken)
                .ConfigureAwait(false);

            return Result.Success<IReadOnlyList<TrackingSessionSummaryDto>>(
                sessions.Select(x => x.ToSummary()).ToArray());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<IReadOnlyList<TrackingSessionSummaryDto>>(
                new Error("Request.Cancelled", "Operation was cancelled."),
                Outcome.Canceled);
        }
    }
}
