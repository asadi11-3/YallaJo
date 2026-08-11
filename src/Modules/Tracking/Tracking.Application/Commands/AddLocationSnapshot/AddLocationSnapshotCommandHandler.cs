using Booking.Contracts.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tracking.Application.Common;
using Tracking.Application.Interfaces;
using Tracking.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Tracking.Application.Commands.AddLocationSnapshot;

public sealed class AddLocationSnapshotCommandHandler(
    ITrackingSessionRepository trackingSessionRepository,
    ITrackingUnitOfWork unitOfWork,
    ITourGuideOwnershipService tourGuideOwnershipService,
    ICurrentUser currentUser,
    ILogger<AddLocationSnapshotCommandHandler> logger)
    : ICommandHandler<AddLocationSnapshotCommand, TrackingLocationSnapshotDto>
{
    public async Task<Result<TrackingLocationSnapshotDto>> Handle(
        AddLocationSnapshotCommand request,
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
                .GetByIdWithDetailsAsync(request.SessionId, cancellationToken)
                .ConfigureAwait(false);

            if (session is null)
            {
                return Result.Failure<TrackingLocationSnapshotDto>(
                    new Error("Tracking.Session.NotFound", "Tracking session was not found."),
                    Outcome.NotFound);
            }

            var canManage = await TrackingAuthorization
                .CanManageSessionAsync(session, actorUserId.Value, currentUser, tourGuideOwnershipService, cancellationToken)
                .ConfigureAwait(false);

            if (!canManage)
            {
                return Result.Failure<TrackingLocationSnapshotDto>(
                    new Error("Tracking.Forbidden", "You are not allowed to update this tracking session."),
                    Outcome.Forbidden);
            }

            session.AddLocationSnapshot(
                request.Latitude,
                request.Longitude,
                request.Accuracy,
                request.Speed,
                request.Heading,
                request.Altitude,
                request.CapturedAt);

            trackingSessionRepository.Update(session);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict adding location to session {SessionId}.", session.Id);
                return Result.Failure<TrackingLocationSnapshotDto>(
                    new Error("Tracking.ConcurrencyConflict", "The session was modified by another request. Retry."),
                    Outcome.Conflict);
            }

            var snapshot = session.LocationSnapshots
                .OrderByDescending(x => x.CapturedAt)
                .First();

            return Result.Success(snapshot.ToDto());
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Result.Failure<TrackingLocationSnapshotDto>(
                new Error("Tracking.InvalidCoordinates", ex.Message),
                Outcome.Invalid);
        }
        catch (BusinessRuleViolationException ex)
        {
            return Result.Failure<TrackingLocationSnapshotDto>(
                new Error("Tracking.InvalidState", ex.Message),
                Outcome.Invalid);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<TrackingLocationSnapshotDto>(
                new Error("Request.Cancelled", "Operation was cancelled."),
                Outcome.Canceled);
        }
    }
}
