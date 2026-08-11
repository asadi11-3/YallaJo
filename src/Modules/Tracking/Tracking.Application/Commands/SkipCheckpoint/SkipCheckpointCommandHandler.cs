using Booking.Contracts.Authorization;
using Microsoft.EntityFrameworkCore;
using Tracking.Application.Common;
using Tracking.Application.Interfaces;
using Tracking.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Tracking.Application.Commands.SkipCheckpoint;

public sealed class SkipCheckpointCommandHandler(
    ITrackingSessionRepository trackingSessionRepository,
    ITrackingUnitOfWork unitOfWork,
    ITourGuideOwnershipService tourGuideOwnershipService,
    ICurrentUser currentUser)
    : ICommandHandler<SkipCheckpointCommand, TrackingCheckpointDto>
{
    public async Task<Result<TrackingCheckpointDto>> Handle(
        SkipCheckpointCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = currentUser.UserId;
            if (!currentUser.IsAuthenticated || actorUserId is null)
            {
                return Result.Failure<TrackingCheckpointDto>(
                    new Error("Tracking.Unauthorized", "Authentication is required."),
                    Outcome.Unauthorized);
            }

            var session = await trackingSessionRepository
                .GetByIdWithDetailsAsync(request.SessionId, cancellationToken)
                .ConfigureAwait(false);

            if (session is null)
            {
                return Result.Failure<TrackingCheckpointDto>(
                    new Error("Tracking.Session.NotFound", "Tracking session was not found."),
                    Outcome.NotFound);
            }

            var canManage = await TrackingAuthorization
                .CanManageSessionAsync(session, actorUserId.Value, currentUser, tourGuideOwnershipService, cancellationToken)
                .ConfigureAwait(false);

            if (!canManage)
            {
                return Result.Failure<TrackingCheckpointDto>(
                    new Error("Tracking.Forbidden", "You are not allowed to update checkpoints for this session."),
                    Outcome.Forbidden);
            }

            var checkpoint = session.TourCheckpoints.FirstOrDefault(x => x.Id == request.CheckpointId);
            if (checkpoint is null)
            {
                return Result.Failure<TrackingCheckpointDto>(
                    new Error("Tracking.Checkpoint.NotFound", "Checkpoint was not found in this session."),
                    Outcome.NotFound);
            }

            session.SkipCheckpoint(checkpoint.WaypointId, request.Notes);
            trackingSessionRepository.Update(session);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<TrackingCheckpointDto>(
                    new Error("Tracking.ConcurrencyConflict", "The session was modified by another request. Retry."),
                    Outcome.Conflict);
            }

            var updatedCheckpoint = session.TourCheckpoints.First(x => x.Id == request.CheckpointId);
            return Result.Success(updatedCheckpoint.ToDto());
        }
        catch (BusinessRuleViolationException ex)
        {
            return Result.Failure<TrackingCheckpointDto>(
                new Error("Tracking.InvalidState", ex.Message),
                Outcome.Invalid);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<TrackingCheckpointDto>(
                new Error("Request.Cancelled", "Operation was cancelled."),
                Outcome.Canceled);
        }
    }
}
