using Booking.Contracts.Authorization;
using Tracking.Application.Common;
using Tracking.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Tracking.Application.Queries.GetLatestLocationByBookingId;

public sealed class GetLatestLocationByBookingIdQueryHandler(
    ITrackingSessionRepository trackingSessionRepository,
    ITourGuideOwnershipService tourGuideOwnershipService,
    ICurrentUser currentUser)
    : IQueryHandler<GetLatestLocationByBookingIdQuery, TrackingLocationSnapshotDto>
{
    public async Task<Result<TrackingLocationSnapshotDto>> Handle(
        GetLatestLocationByBookingIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = currentUser.UserId;
            if (!currentUser.IsAuthenticated || actorUserId is null)
            {
                return Result.Failure<TrackingLocationSnapshotDto>(
                    new Error("Tracking.Unauthorized", "Authentication is required."),
                    Outcome.Unauthorized);
            }

            var session = await trackingSessionRepository
                .GetActiveByBookingIdAsync(request.TourBookingId, cancellationToken)
                .ConfigureAwait(false);

            if (session is null)
            {
                return Result.Failure<TrackingLocationSnapshotDto>(
                    new Error("Tracking.Session.NotFound", "Active tracking session was not found."),
                    Outcome.NotFound);
            }

            var canRead = await TrackingAuthorization
                .CanReadSessionAsync(session, actorUserId.Value, currentUser, tourGuideOwnershipService, cancellationToken)
                .ConfigureAwait(false);

            if (!canRead)
            {
                return Result.Failure<TrackingLocationSnapshotDto>(
                    new Error("Tracking.Forbidden", "You are not allowed to view this tracking session."),
                    Outcome.Forbidden);
            }
            
            var snapshot = session.LocationSnapshots
                .OrderByDescending(x => x.CapturedAt)
                .FirstOrDefault();

            if (snapshot is null)
            {
                return Result.Failure<TrackingLocationSnapshotDto>(
                    new Error("Tracking.Location.NotFound", "No location snapshots have been recorded yet."),
                    Outcome.NotFound);
            }

            return Result.Success(snapshot.ToDto());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<TrackingLocationSnapshotDto>(
                new Error("Request.Cancelled", "Operation was cancelled."),
                Outcome.Canceled);
        }
    }
}
