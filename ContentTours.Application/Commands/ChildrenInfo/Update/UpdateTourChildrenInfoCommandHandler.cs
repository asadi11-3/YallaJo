using ContentTours.Application.Caching;
using ContentTours.Application.Commands.Shared;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.ChildrenInfo.Update;

public sealed class UpdateTourChildrenInfoCommandHandler(
    ITourRepository tourRepository,
    IContentToursEventUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateTourChildrenInfoCommandHandler> logger)
    : ICommandHandler<UpdateTourChildrenInfoCommand>
{
    public async Task<Result> Handle(UpdateTourChildrenInfoCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result.Failure(
                    Error.Unauthorized("Authentication is required."),
                    Outcome.Unauthorized);
            }

            var tour = await tourRepository
                .GetByIdAsync(request.TourId, cancellationToken, asNoTracking: false)
                .ConfigureAwait(false);

            if (tour is null || tour.IsDeleted)
            {
                return Result.Failure(
                    new Error("Tour.NotFound", $"Tour '{request.TourId}' was not found."),
                    Outcome.NotFound);
            }

            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;

            if (!isAdminTier && tour.CreatedByUserId != currentUser.UserId.Value)
            {
                return Result.Failure(
                    new Error("Tour.NotOwner", "You do not have permission to update this tour."),
                    Outcome.Forbidden);
            }

            if (!ChildFacilitiesParser.TryParse(request.ChildFacilities, out var facilities, out var unknownToken))
            {
                return Result.Failure(
                    new Error(
                        "Tour.UnknownChildFacility",
                        $"Unknown child facility: '{unknownToken}'."),
                    Outcome.Invalid);
            }

            try
            {
                tour.UpdateChildrenInfo(
                    allowsChildren:   request.AllowsChildren,
                    minChildAge:      request.MinChildAge,
                    maxChildAge:      request.MaxChildAge,
                    childFacilities:  facilities);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure(
                    new Error("Tour.ChildrenInfoInvalid", ex.Message),
                    Outcome.Invalid);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(
                    new Error("Tour.InvalidTransition", ex.Message),
                    Outcome.Conflict);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Tour.ConcurrencyConflict",
                        "This tour was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourChildrenInfoCacheKeys.TagForTour(tour.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForTour(tour.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagToursList, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagToursSearch, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Tour ChildrenInfo updated: {TourId} (AllowsChildren={AllowsChildren}, FacilityCount={FacilityCount}, By={UserId})",
                tour.Id,
                tour.AllowsChildren,
                facilities.Count,
                currentUser.UserId);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
