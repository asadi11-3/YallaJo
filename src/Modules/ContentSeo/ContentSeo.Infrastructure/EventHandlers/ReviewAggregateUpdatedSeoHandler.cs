using ContentSeo.Application.Interfaces;
using ContentSeo.Application.Seo;
using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Enums;
using ContentSeo.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Social.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

public sealed class ReviewAggregateUpdatedSeoHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<ReviewAggregateUpdatedSeoHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ReviewAggregateUpdatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<ReviewAggregateUpdatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        if (!TryMapEntityType(evt.EntityType, out var seoEntityType))
        {
            logger.LogDebug("ContentSeo: ignoring rating aggregate for unsupported entity type {EntityType}", evt.EntityType);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var metadata = await dbContext.SeoMetadata.FirstOrDefaultAsync(s => s.EntityType == seoEntityType && s.EntityId == evt.EntityId, ct);
        if (metadata is null)
        {
            metadata = SeoMetadata.Create(seoEntityType, evt.EntityId);
            dbContext.SeoMetadata.Add(metadata);
        }

        var merged = SchemaMarkupMerger.MergeAggregateRating(
            metadata.SchemaMarkup,
            evt.AverageRating,
            evt.ReviewCount,
            evt.BestRating,
            evt.WorstRating);
        metadata.UpdateSchema(merged);

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("ContentSeo: schema AggregateRating updated for {EntityType} {EntityId}", seoEntityType, evt.EntityId);
    }

    private static bool TryMapEntityType(string entityType, out SeoEntityType seoEntityType)
    {
        if (Enum.TryParse(entityType, ignoreCase: true, out seoEntityType))
        {
            return seoEntityType is SeoEntityType.Place or SeoEntityType.Tour or SeoEntityType.Business or SeoEntityType.TourGuide;
        }

        seoEntityType = default;
        return false;
    }
}
