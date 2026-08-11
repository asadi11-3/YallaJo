using Booking.Contracts.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tracking.Application.Common;
using Tracking.Application.Interfaces;
using Tracking.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Tracking.Application.Commands.StartLiveTrackingSession;

public sealed class StartLiveTrackingSessionCommandHandler(
    ITrackingSessionRepository trackingSessionRepository,
    ITrackingUnitOfWork unitOfWork,
    ITourGuideOwnershipService tourGuideOwnershipService,
    ICurrentUser currentUser,
    ILogger<StartLiveTrackingSessionCommandHandler> logger)
    : ICommandHandler<StartLiveTrackingSessionCommand, TrackingSessionSummaryDto>
{
    public async Task<Result<TrackingSessionSummaryDto>> Handle(
        StartLiveTrackingSessionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = currentUser.UserId;
            if (!currentUser.IsAuthenticated || actorUserId is null)
            {
                return Result.Failure<TrackingSessionSummaryDto>(
                    new Error("Tracking.Unauthorized", "Authentication is required."),
                    Outcome.Unauthorized);
            }

            if (!TrackingAuthorization.IsAdmin(currentUser))
            {
                var guideOwnership = await tourGuideOwnershipService
                    .GetTourGuideOwnershipAsync(request.TourGuideId, cancellationToken)
                    .ConfigureAwait(false);

                if (!guideOwnership.Exists || guideOwnership.IsDeleted)
                {
                    return Result.Failure<TrackingSessionSummaryDto>(
                        new Error("Tracking.TourGuide.NotFound", "Assigned tour guide was not found."),
                        Outcome.NotFound);
                }

                if (guideOwnership.OwnerUserId != actorUserId.Value)
                {
                    return Result.Failure<TrackingSessionSummaryDto>(
                        new Error("Tracking.Forbidden", "You are not allowed to start tracking for this guide."),
                        Outcome.Forbidden);
                }
            }

            var hasActiveSession = await trackingSessionRepository
                .HasActiveSessionForBookingAsync(request.TourBookingId, cancellationToken)
                .ConfigureAwait(false);

            if (hasActiveSession)
            {
                return Result.Failure<TrackingSessionSummaryDto>(
                    new Error("Tracking.ActiveSessionExists", "A live tracking session already exists for this booking."),
                    Outcome.Conflict);
            }

            var session = Tracking.Domain.Entities.LiveTrackingSession.Start(
                request.UserId,
                request.TourBookingId,
                request.TourGuideId,
                request.StartedAt);

            foreach (var waypointId in request.WaypointIds?.Distinct() ?? [])
            {
                session.AddCheckpoint(waypointId);
            }

            await trackingSessionRepository.AddAsync(session, cancellationToken).ConfigureAwait(false);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict starting tracking for booking {BookingId}.", request.TourBookingId);
                return Result.Failure<TrackingSessionSummaryDto>(
                    new Error("Tracking.ConcurrencyConflict", "The session was modified by another request. Retry."),
                    Outcome.Conflict);
            }

            logger.LogInformation(
                "Tracking session {SessionId} started for booking {BookingId} by actor {ActorUserId}.",
                session.Id,
                session.TourBookingId,
                actorUserId.Value);

            return Result.Created(session.ToSummary());
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<TrackingSessionSummaryDto>(
                new Error("Tracking.InvalidInput", ex.Message),
                Outcome.Invalid);
        }
        catch (YallaJo.SharedKernel.Domain.Exceptions.BusinessRuleViolationException ex)
        {
            return Result.Failure<TrackingSessionSummaryDto>(
                new Error("Tracking.InvalidState", ex.Message),
                Outcome.Invalid);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<TrackingSessionSummaryDto>(
                new Error("Request.Cancelled", "Operation was cancelled."),
                Outcome.Canceled);
        }
    }
}
