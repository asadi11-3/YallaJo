using Booking.Contracts.Authorization;
using Tracking.Application.Common;
using Tracking.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Tracking.Application.Queries.GetTrackingSessionDetails;

public sealed class GetTrackingSessionDetailsQueryHandler(
    ITrackingSessionRepository trackingSessionRepository,
    ITourGuideOwnershipService tourGuideOwnershipService,
    ICurrentUser currentUser)
    : IQueryHandler<GetTrackingSessionDetailsQuery, TrackingSessionDetailsDto>
{
    public async Task<Result<TrackingSessionDetailsDto>> Handle(
        GetTrackingSessionDetailsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = currentUser.UserId;
            if (!currentUser.IsAuthenticated || actorUserId is null)
            {
                return Result.Failure<TrackingSessionDetailsDto>(
                    new Error("Tracking.Unauthorized", "Authentication is required."),
                    Outcome.Unauthorized);
            }

            var session = await trackingSessionRepository
                .GetByIdWithDetailsAsync(request.SessionId, cancellationToken)
                .ConfigureAwait(false);

            if (session is null)
            {
                return Result.Failure<TrackingSessionDetailsDto>(
                    new Error("Tracking.Session.NotFound", "Tracking session was not found."),
                    Outcome.NotFound);
            }

            var canRead = await TrackingAuthorization
                .CanReadSessionAsync(session, actorUserId.Value, currentUser, tourGuideOwnershipService, cancellationToken)
                .ConfigureAwait(false);

            if (!canRead)
            {
                return Result.Failure<TrackingSessionDetailsDto>(
                    new Error("Tracking.Forbidden", "You are not allowed to view this tracking session."),
                    Outcome.Forbidden);
            }

            return Result.Success(session.ToDetails());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<TrackingSessionDetailsDto>(
                new Error("Request.Cancelled", "Operation was cancelled."),
                Outcome.Canceled);
        }
    }
}
