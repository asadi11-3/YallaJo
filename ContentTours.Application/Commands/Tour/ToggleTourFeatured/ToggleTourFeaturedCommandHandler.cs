using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.Tour.ToggleTourFeatured;

public sealed class ToggleTourFeaturedCommandHandler(
    ITourRepository tourRepo,
    IContentToursEventUnitOfWork unitOfWork,   // event-dispatching UoW — Tour raises domain event
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<ToggleTourFeaturedCommandHandler> logger)
    : ICommandHandler<ToggleTourFeaturedCommand>
{
    public async Task<Result> Handle(ToggleTourFeaturedCommand cmd, CancellationToken ct)
    {
        var tour = await tourRepo.GetByIdAsync(cmd.TourId, ct);
        if (tour is null || tour.IsDeleted)
            return Result.NotFound("Tour.NotFound");

        // Only Approved tours can be featured (IsApproved() centralises PW-1 mapping)
        if (!tour.Status.IsApproved())
            return Result.Fail(
                Outcome.Conflict,
                new Error("Tour.CannotFeatureNonApproved",
                    "Only Approved tours can be featured."));

        var willChange = tour.IsFeatured != cmd.IsFeatured;

        // SetFeatured is idempotent — no event if value unchanged
        tour.SetFeatured(cmd.IsFeatured, currentUser.UserId!.Value);

        await unitOfWork.SaveChangesAsync(ct);   // dispatches TourFeaturedChangedDomainEvent if changed

        if (willChange)
        {
            await cache.RemoveByTagAsync("tours:featured", ct);
            await cache.RemoveByTagAsync(TourCacheKeys.TagForTour(tour.Id), ct);
            await cache.RemoveByTagAsync("tours:list", ct);
            await cache.RemoveByTagAsync("tours:search", ct);
            // Provider's MyTours dashboard shows IsFeatured — bust their cache too
            await cache.RemoveByTagAsync($"my-tours:{tour.CreatedByUserId}", ct);

            logger.LogInformation(
                "Tour {TourId} featured set to {IsFeatured} by {UserId}",
                tour.Id, cmd.IsFeatured, currentUser.UserId);
        }

        return Result.Success();
    }
}
