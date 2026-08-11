using Booking.Contracts.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tracking.Application.Common;
using Tracking.Application.Interfaces;
using Tracking.Domain.Enums;
using Tracking.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Tracking.Application.Commands.EndLiveTrackingSession;

public sealed class EndLiveTrackingSessionCommandHandler(
    ITrackingSessionRepository trackingSessionRepository,
    ITrackingUnitOfWork unitOfWork,
    ITourGuideOwnershipService tourGuideOwnershipService,
    ICurrentUser currentUser,
    ILogger<EndLiveTrackingSessionCommandHandler> logger)
    : ICommandHandler<EndLiveTrackingSessionCommand, TrackingSessionSummaryDto>
{
    public async Task<Result<TrackingSessionSummaryDto>> Handle(
        EndLiveTrackingSessionCommand request,
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

            var session = await trackingSessionRepository
                .GetByIdWithDetailsAsync(request.SessionId, cancellationToken)
                .ConfigureAwait(false);

            if (session is null)
            {
                return Result.Failure<TrackingSessionSummaryDto>(
                    new Error("Tracking.Session.NotFound", "Tracking session was not found."),
                    Outcome.NotFound);
            }

            var canManage = await TrackingAuthorization
                .CanManageSessionAsync(session, actorUserId.Value, currentUser, tourGuideOwnershipService, cancellationToken)
                .ConfigureAwait(false);

            if (!canManage)
            {
                return Result.Failure<TrackingSessionSummaryDto>(
                    new Error("Tracking.Forbidden", "You are not allowed to end this tracking session."),
                    Outcome.Forbidden);
            }

            if (session.Status == SessionStatus.Completed)
            {
                return Result.Success(session.ToSummary());
            }

            session.End(request.Reason, request.EndedAt);
            trackingSessionRepository.Update(session);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict ending session {SessionId}.", session.Id);
                return Result.Failure<TrackingSessionSummaryDto>(
                    new Error("Tracking.ConcurrencyConflict", "The session was modified by another request. Retry."),
                    Outcome.Conflict);
            }

            return Result.Success(session.ToSummary());
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<TrackingSessionSummaryDto>(
                new Error("Tracking.InvalidInput", ex.Message),
                Outcome.Invalid);
        }
        catch (BusinessRuleViolationException ex)
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
