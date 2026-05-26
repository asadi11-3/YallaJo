using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Social.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Infrastructure.EventHandlers;

/// <summary>
/// Handles <see cref="RatingRecalculatedIntegrationEvent"/> from Social module.
/// Updates the rating on the target Place or Business entity based on TargetType.
/// </summary>
public sealed class RatingRecalculatedUpdateEntityHandler(
    IPlaceRepository placeRepository,
    IBusinessRepository businessRepository,
    IContentPlacesUnitOfWork unitOfWork,
    IContentPlacesInboxStore inboxStore,
    ILogger<RatingRecalculatedUpdateEntityHandler> logger)
    : INotificationHandler<IntegrationEventNotification<RatingRecalculatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<RatingRecalculatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentPlaces: Message {MessageId} (RatingRecalculated) already processed; skipping.",
                notification.MessageId);
            return;
        }

        var evt = notification.Event;

        switch (evt.TargetType.ToLowerInvariant())
        {
            case "place":
            {
                var place = await placeRepository.GetByIdAsync(evt.TargetId, ct, asNoTracking: false);
                if (place is null)
                {
                    logger.LogWarning(
                        "RatingRecalculated: Place {PlaceId} not found; skipping.",
                        evt.TargetId);
                }
                else
                {
                    place.UpdateRating(evt.NewAverageRating, evt.ReviewCount);
                    logger.LogInformation(
                        "RatingRecalculated: Place {PlaceId} rating updated to {Rating} ({Count} reviews).",
                        evt.TargetId, evt.NewAverageRating, evt.ReviewCount);
                }

                break;
            }

            case "business":
            {
                var business = await businessRepository.GetByIdAsync(evt.TargetId, ct, asNoTracking: false);
                if (business is null)
                {
                    logger.LogWarning(
                        "RatingRecalculated: Business {BusinessId} not found; skipping.",
                        evt.TargetId);
                }
                else
                {
                    business.UpdateRating(evt.NewAverageRating, evt.ReviewCount);
                    logger.LogInformation(
                        "RatingRecalculated: Business {BusinessId} rating updated to {Rating} ({Count} reviews).",
                        evt.TargetId, evt.NewAverageRating, evt.ReviewCount);
                }

                break;
            }

            default:
                logger.LogDebug(
                    "RatingRecalculated: TargetType '{TargetType}' is not handled by ContentPlaces; skipping.",
                    evt.TargetType);
                break;
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
