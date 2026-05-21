using MediatR;
using Microsoft.Extensions.Logging;
using Social.Contracts.IntegrationEvents;
using Social.Domain.Events;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Infrastructure.EventHandlers;

/// <summary>
/// Converts <see cref="ReviewPublishedDomainEvent"/> into <c>social.review.published.v1</c>.
/// Looks up the Review aggregate to read IsVerifiedBooking (not carried on the domain event).
/// Dispatched BEFORE SaveChanges inside the same UoW so the OutboxMessage row is persisted atomically.
/// </summary>
internal sealed class PublishReviewPublishedHandler(
    ISocialOutboxWriter outbox,
    IReviewRepository reviewRepository,
    ILogger<PublishReviewPublishedHandler> logger)
    : INotificationHandler<DomainEventNotification<ReviewPublishedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<ReviewPublishedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        var review = await reviewRepository.GetByIdAsync(e.ReviewId, ct).ConfigureAwait(false);
        var isVerifiedBooking = review?.IsVerifiedBooking ?? false;

        var integration = new ReviewPublishedIntegrationEvent(
            ReviewId: e.ReviewId,
            UserId: e.UserId,
            TargetType: e.TargetType.ToString(),
            TargetId: e.TargetId,
            Rating: e.Rating,
            IsVerifiedBooking: isVerifiedBooking,
            PublishedAt: e.PublishedAt);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued social.review.published.v1 for Review {ReviewId}", e.ReviewId);
    }
}

/// <summary>Converts <see cref="ReviewDeletedDomainEvent"/> into <c>social.review.deleted.v1</c>.</summary>
internal sealed class PublishReviewDeletedHandler(
    ISocialOutboxWriter outbox,
    ILogger<PublishReviewDeletedHandler> logger)
    : INotificationHandler<DomainEventNotification<ReviewDeletedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<ReviewDeletedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new ReviewDeletedIntegrationEvent(
            ReviewId: e.ReviewId,
            UserId: e.UserId,
            TargetType: e.TargetType.ToString(),
            TargetId: e.TargetId,
            DeletedAt: e.DeletedAt,
            DeletionSource: e.Source.ToString());
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued social.review.deleted.v1 for Review {ReviewId} (source={Source})", e.ReviewId, e.Source);
    }
}

/// <summary>Converts <see cref="FavoriteAddedDomainEvent"/> into <c>social.favorite.added.v1</c>.</summary>
internal sealed class PublishFavoriteAddedHandler(
    ISocialOutboxWriter outbox,
    ILogger<PublishFavoriteAddedHandler> logger)
    : INotificationHandler<DomainEventNotification<FavoriteAddedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<FavoriteAddedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new FavoriteAddedIntegrationEvent(
            FavoriteId: e.FavoriteId,
            UserId: e.UserId,
            EntityType: e.EntityType.ToString(),
            EntityId: e.EntityId,
            AddedAt: e.AddedAt);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued social.favorite.added.v1 for Favorite {FavoriteId}", e.FavoriteId);
    }
}

/// <summary>
/// Converts <see cref="ReportResolvedDomainEvent"/> into <c>social.report.resolved.v1</c>.
/// Looks up the Report aggregate to read EntityType / EntityId (not carried on the domain event).
/// </summary>
internal sealed class PublishReportResolvedHandler(
    ISocialOutboxWriter outbox,
    IReportRepository reportRepository,
    ILogger<PublishReportResolvedHandler> logger)
    : INotificationHandler<DomainEventNotification<ReportResolvedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<ReportResolvedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        var report = await reportRepository.GetByIdAsync(e.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning(
                "Social: Report {ReportId} not found during ReportResolved converter — skipping integration event emission.",
                e.ReportId);
            return;
        }

        var integration = new ReportResolvedIntegrationEvent(
            ReportId: e.ReportId,
            AdminUserId: e.AdminUserId,
            EntityType: report.EntityType.ToString(),
            EntityId: report.EntityId,
            Action: e.Action.ToString(),
            ResolvedAt: e.ResolvedAt);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued social.report.resolved.v1 for Report {ReportId} (action={Action})", e.ReportId, e.Action);
    }
}

/// <summary>
/// Converts <see cref="EntityRatingRecalculatedDomainEvent"/> into <c>social.rating.recalculated.v1</c>.
/// Looks up the EntityRatingCache aggregate to read BayesianScore (not carried on the domain event).
/// </summary>
internal sealed class PublishRatingRecalculatedHandler(
    ISocialOutboxWriter outbox,
    IEntityRatingCacheRepository ratingCacheRepository,
    ILogger<PublishRatingRecalculatedHandler> logger)
    : INotificationHandler<DomainEventNotification<EntityRatingRecalculatedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<EntityRatingRecalculatedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        var cache = await ratingCacheRepository.GetByTargetAsync(e.TargetType, e.TargetId, ct).ConfigureAwait(false);
        var bayesian = cache?.BayesianScore ?? e.NewAverageRating;

        var integration = new RatingRecalculatedIntegrationEvent(
            TargetType: e.TargetType.ToString(),
            TargetId: e.TargetId,
            NewAverageRating: e.NewAverageRating,
            NewBayesianScore: bayesian,
            ReviewCount: e.NewReviewCount,
            RecalculatedAt: e.RecalculatedAt);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation(
            "Enqueued social.rating.recalculated.v1 for ({TargetType}, {TargetId}) — avg={Avg} bayes={Bayes} n={N}",
            e.TargetType, e.TargetId, e.NewAverageRating, bayesian, e.NewReviewCount);
    }
}
