using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourGuides.AddSpecialization;

public sealed class AddTourGuideSpecializationCommandHandler(
    ITourGuideRepository guideRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<AddTourGuideSpecializationCommandHandler> logger)
    : ICommandHandler<AddTourGuideSpecializationCommand>
{
    public async Task<Result> Handle(AddTourGuideSpecializationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is not Guid callerUserId)
            {
                return Result.Failure(
                    new Error("Auth.Unauthorized", "An authenticated user is required."),
                    Outcome.Unauthorized);
            }

            var guide = await guideRepository
                .GetWithDetailsAsync(request.TourGuideId, cancellationToken, asNoTracking: false)
                .ConfigureAwait(false);

            if (guide is null)
            {
                return Result.Failure(
                    new Error("TourGuide.NotFound", $"Tour guide '{request.TourGuideId}' was not found."),
                    Outcome.NotFound);
            }

            if (guide.UserId != callerUserId)
            {
                return Result.Failure(
                    new Error("TourGuide.NotOwner", "You do not have permission to update this guide profile."),
                    Outcome.Forbidden);
            }

            try
            {
                guide.AddSpecialization(request.SpecializationId);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure(new Error("TourGuideSpecialization.Invalid", ex.Message), Outcome.Invalid);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(ToConflictError(ex), Outcome.Conflict);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict adding specialization to TourGuideId={TourGuideId}", guide.Id);
                return Result.Failure(
                    new Error("TourGuide.ConcurrencyConflict", "This guide profile was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForProfile(guide.Id), cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Added SpecializationId={SpecializationId} to TourGuideId={TourGuideId} by UserId={UserId}",
                request.SpecializationId,
                guide.Id,
                callerUserId);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }

    private static Error ToConflictError(InvalidOperationException ex)
    {
        var message = ex.Message;
        var separator = message.IndexOf(':', StringComparison.Ordinal);
        if (separator <= 0)
        {
            return new Error("TourGuideSpecialization.InvalidState", message);
        }

        return new Error(message[..separator], message[(separator + 1)..].Trim());
    }
}
