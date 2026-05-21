using Analytics.Application.Interfaces;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Analytics.Infrastructure.Persistence;
using Auth.Contracts.IntegrationEvents;
using Booking.Contracts.IntegrationEvents;
using ContentPlaces.Contracts.IntegrationEvents;
using ContentTours.Contracts;
using Finance.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Contracts.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Social.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Infrastructure.EventHandlers;

internal static class AnalyticsHandlerOps
{
    public static async Task<PopularityScore> GetOrCreateScoreAsync(AnalyticsDbContext db, EntityType type, Guid id, DateTime now, CancellationToken ct)
    {
        var score = await db.PopularityScores.FirstOrDefaultAsync(x => x.EntityType == type && x.EntityId == id, ct);
        if (score is not null) return score;
        score = PopularityScore.Initialize(type, id, now);
        await db.PopularityScores.AddAsync(score, ct);
        return score;
    }

    public static async Task AddAuditAsync(AnalyticsDbContext db, Guid? userId, AuditLogAction action, string entityType, Guid entityId, string? oldValue, string? newValue, DateTime now, CancellationToken ct)
        => await db.AuditLogs.AddAsync(AuditLog.Append(userId, null, action, null, entityType, entityId, oldValue, newValue, null, null, null, now), ct);
}

public sealed class BookingTourBookingCreatedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<BookingTourBookingCreatedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourBookingCreatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourBookingCreatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        await db.UserInteractions.AddAsync(UserInteraction.Record(evt.UserId, null, EntityType.Tour, evt.TourId, InteractionType.BookingStarted, evt.BookedAt, null, null), ct);
        if (!await db.BookingSnapshots.AnyAsync(x => x.BookingId == evt.BookingId, ct))
            await db.BookingSnapshots.AddAsync(BookingSnapshot.Create(evt.BookingId, evt.UserId, evt.ProviderId, evt.TourId, "AwaitingPayment", evt.TotalAmount, evt.Currency, evt.BookedAt), ct);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics recorded booking started for {BookingId}", evt.BookingId);
    }
}

public sealed class BookingTourBookingConfirmedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<BookingTourBookingConfirmedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourBookingConfirmedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourBookingConfirmedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        var snapshot = await db.BookingSnapshots.FirstOrDefaultAsync(x => x.BookingId == evt.BookingId, ct);
        snapshot?.MarkConfirmed();
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics confirmed booking snapshot {BookingId}", evt.BookingId);
    }
}

public sealed class BookingTourBookingCancelledHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<BookingTourBookingCancelledHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourBookingCancelledIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourBookingCancelledIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        var snapshot = await db.BookingSnapshots.FirstOrDefaultAsync(x => x.BookingId == evt.BookingId, ct);
        snapshot?.MarkCancelled(evt.CancelledAt);
        await AnalyticsHandlerOps.AddAuditAsync(db, evt.UserId, AuditLogAction.Cancel, nameof(BookingSnapshot), evt.BookingId, null, evt.Reason, evt.CancelledAt, ct);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics cancelled booking snapshot {BookingId}", evt.BookingId);
    }
}

public sealed class BookingTourBookingCompletedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<BookingTourBookingCompletedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourBookingCompletedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourBookingCompletedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        await db.UserInteractions.AddAsync(UserInteraction.Record(evt.UserId, null, EntityType.Tour, evt.TourId, InteractionType.BookingCompleted, evt.CompletedAt, null, null), ct);
        var snapshot = await db.BookingSnapshots.FirstOrDefaultAsync(x => x.BookingId == evt.BookingId, ct);
        snapshot?.MarkCompleted(evt.CompletedAt);
        var score = await AnalyticsHandlerOps.GetOrCreateScoreAsync(db, EntityType.Tour, evt.TourId, evt.CompletedAt, ct);
        score.MarkStale(evt.CompletedAt);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics completed booking snapshot {BookingId}", evt.BookingId);
    }
}

public sealed class FinancePaymentCompletedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<FinancePaymentCompletedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PaymentCompletedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<PaymentCompletedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        if (!await db.PaymentSnapshots.AnyAsync(x => x.PaymentId == evt.PaymentId, ct))
            await db.PaymentSnapshots.AddAsync(PaymentSnapshot.Completed(evt.PaymentId, evt.BookingId, evt.UserId, evt.ProviderId, evt.Amount, evt.Currency, evt.CompletedAt), ct);
        await AnalyticsHandlerOps.AddAuditAsync(db, evt.UserId, AuditLogAction.Confirm, nameof(PaymentSnapshot), evt.PaymentId, null, evt.Amount.ToString("F2"), evt.CompletedAt, ct);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics recorded payment {PaymentId}", evt.PaymentId);
    }
}

public sealed class FinancePayoutCompletedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<FinancePayoutCompletedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PayoutCompletedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<PayoutCompletedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        if (!await db.PaymentSnapshots.AnyAsync(x => x.PaymentId == evt.PayoutId, ct))
            await db.PaymentSnapshots.AddAsync(PaymentSnapshot.Payout(evt.PayoutId, evt.ProviderId, evt.NetAmount, evt.Currency, evt.CompletedAt), ct);
        await AnalyticsHandlerOps.AddAuditAsync(db, evt.ProviderId, AuditLogAction.Confirm, nameof(PaymentSnapshot), evt.PayoutId, null, evt.NetAmount.ToString("F2"), evt.CompletedAt, ct);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics recorded payout {PayoutId}", evt.PayoutId);
    }
}

public sealed class FinanceRefundCompletedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<FinanceRefundCompletedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<RefundCompletedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<RefundCompletedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        if (!await db.PaymentSnapshots.AnyAsync(x => x.PaymentId == evt.RefundPaymentId, ct))
            await db.PaymentSnapshots.AddAsync(PaymentSnapshot.Refund(evt.RefundPaymentId, evt.OriginalPaymentId, evt.BookingId, evt.Amount, evt.Currency, evt.CompletedAt), ct);
        await AnalyticsHandlerOps.AddAuditAsync(db, null, AuditLogAction.Refund, nameof(PaymentSnapshot), evt.RefundPaymentId, null, evt.Reason, evt.CompletedAt, ct);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics recorded refund {RefundPaymentId}", evt.RefundPaymentId);
    }
}

public sealed class SocialReviewPublishedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<SocialReviewPublishedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ReviewPublishedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<ReviewPublishedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        if (Enum.TryParse<EntityType>(evt.TargetType, true, out var type))
        {
            await db.UserInteractions.AddAsync(UserInteraction.Record(evt.UserId, null, type, evt.TargetId, InteractionType.ReviewSubmitted, evt.PublishedAt, null, null), ct);
            var score = await AnalyticsHandlerOps.GetOrCreateScoreAsync(db, type, evt.TargetId, evt.PublishedAt, ct);
            score.MarkStale(evt.PublishedAt);
        }
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics recorded review {ReviewId}", evt.ReviewId);
    }
}

public sealed class SocialReviewDeletedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<SocialReviewDeletedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ReviewDeletedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<ReviewDeletedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        if (Enum.TryParse<EntityType>(evt.TargetType, true, out var type))
        {
            var score = await AnalyticsHandlerOps.GetOrCreateScoreAsync(db, type, evt.TargetId, evt.DeletedAt, ct);
            score.MarkStale(evt.DeletedAt);
        }
        await AnalyticsHandlerOps.AddAuditAsync(db, evt.UserId, AuditLogAction.Delete, "Review", evt.ReviewId, null, evt.DeletionSource, evt.DeletedAt, ct);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics recorded review deletion {ReviewId} (source={Source})", evt.ReviewId, evt.DeletionSource);
    }
}

public sealed class SocialFavoriteAddedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<SocialFavoriteAddedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<FavoriteAddedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<FavoriteAddedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        if (Enum.TryParse<EntityType>(evt.EntityType, true, out var type))
        {
            await db.UserInteractions.AddAsync(UserInteraction.Record(evt.UserId, null, type, evt.EntityId, InteractionType.AddToFavorite, evt.AddedAt, null, null), ct);
            var score = await AnalyticsHandlerOps.GetOrCreateScoreAsync(db, type, evt.EntityId, evt.AddedAt, ct);
            score.MarkStale(evt.AddedAt);
        }
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics recorded favorite {FavoriteId}", evt.FavoriteId);
    }
}

public sealed class SocialRatingRecalculatedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<SocialRatingRecalculatedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<RatingRecalculatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<RatingRecalculatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        if (Enum.TryParse<EntityType>(evt.TargetType, true, out var type))
        {
            var score = await AnalyticsHandlerOps.GetOrCreateScoreAsync(db, type, evt.TargetId, evt.RecalculatedAt, ct);
            score.UpdateRatingSnapshot(evt.NewAverageRating, evt.ReviewCount, evt.RecalculatedAt);
        }
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics updated rating snapshot for {TargetType}/{TargetId}", evt.TargetType, evt.TargetId);
    }
}

public sealed class ContentToursCreatedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<ContentToursCreatedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourCreatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourCreatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        await AnalyticsHandlerOps.GetOrCreateScoreAsync(db, EntityType.Tour, evt.TourId, DateTime.UtcNow, ct);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics initialized tour popularity {TourId}", evt.TourId);
    }
}

public sealed class ContentToursDeletedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<ContentToursDeletedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourDeletedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourDeletedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        var score = await db.PopularityScores.FirstOrDefaultAsync(x => x.EntityType == EntityType.Tour && x.EntityId == evt.TourId, ct);
        score?.SoftDelete(evt.DeletedAt);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics soft-deleted tour popularity {TourId}", evt.TourId);
    }
}

public sealed class ContentPlacesCreatedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<ContentPlacesCreatedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PlaceCreatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<PlaceCreatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        await AnalyticsHandlerOps.GetOrCreateScoreAsync(db, EntityType.Place, evt.PlaceId, DateTime.UtcNow, ct);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics initialized place popularity {PlaceId}", evt.PlaceId);
    }
}

public sealed class ContentPlacesDeletedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<ContentPlacesDeletedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PlaceDeletedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<PlaceDeletedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        var score = await db.PopularityScores.FirstOrDefaultAsync(x => x.EntityType == EntityType.Place && x.EntityId == evt.PlaceId, ct);
        score?.SoftDelete(DateTime.UtcNow);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics soft-deleted place popularity {PlaceId}", evt.PlaceId);
    }
}

public sealed class AuthUserRegisteredHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<AuthUserRegisteredHandler> logger)
    : INotificationHandler<IntegrationEventNotification<UserRegisteredIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<UserRegisteredIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        await AnalyticsHandlerOps.AddAuditAsync(db, evt.UserId, AuditLogAction.Create, "User", evt.UserId, null, evt.Email, evt.RegisteredAt, ct);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics audit logged user registration {UserId}", evt.UserId);
    }
}

public sealed class BookingAvailabilitySlotCapacityChangedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<BookingAvailabilitySlotCapacityChangedHandler> logger, TimeProvider timeProvider)
    : INotificationHandler<IntegrationEventNotification<AvailabilitySlotCapacityChangedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<AvailabilitySlotCapacityChangedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await AnalyticsHandlerOps.AddAuditAsync(db, null, AuditLogAction.Update, "AvailabilitySlot", evt.SlotId, evt.OldCapacity.ToString(), evt.NewCapacity.ToString(), now, ct);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics audit logged slot capacity change {SlotId} {Old}->{New}", evt.SlotId, evt.OldCapacity, evt.NewCapacity);
    }
}

public sealed class FinanceCommissionRuleUpsertedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<FinanceCommissionRuleUpsertedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CommissionRuleUpsertedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<CommissionRuleUpsertedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        var summary = $"{evt.Tier}|{evt.Currency}|{evt.MinMonthlyRevenue:F2}-{(evt.MaxMonthlyRevenue?.ToString("F2") ?? "*")}|{evt.Percentage:F2}%";
        await AnalyticsHandlerOps.AddAuditAsync(db, null, AuditLogAction.Update, "CommissionRule", evt.RuleId, null, summary, evt.OccurredAt, ct);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics audit logged commission rule upsert {RuleId}", evt.RuleId);
    }
}

public sealed class FinanceCommissionRuleDeletedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<FinanceCommissionRuleDeletedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CommissionRuleDeletedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<CommissionRuleDeletedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        await AnalyticsHandlerOps.AddAuditAsync(db, null, AuditLogAction.Delete, "CommissionRule", evt.RuleId, null, null, evt.OccurredAt, ct);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics audit logged commission rule deletion {RuleId}", evt.RuleId);
    }
}

public sealed class MessagingTicketCreatedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<MessagingTicketCreatedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TicketCreatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TicketCreatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        var summary = $"{evt.Category}|{evt.Priority}|{evt.Subject}";
        await AnalyticsHandlerOps.AddAuditAsync(db, evt.CreatedByUserId, AuditLogAction.Create, "SupportTicket", evt.TicketId, null, summary, evt.CreatedAt, ct);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics audit logged support ticket creation {TicketId}", evt.TicketId);
    }
}

public sealed class MessagingTicketAssignedHandler(AnalyticsDbContext db, IAnalyticsInboxStore inboxStore, IAnalyticsUnitOfWork unitOfWork, ILogger<MessagingTicketAssignedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TicketAssignedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TicketAssignedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        await AnalyticsHandlerOps.AddAuditAsync(db, evt.AssignedByUserId, AuditLogAction.Update, "SupportTicket", evt.TicketId, null, evt.AssignedToUserId.ToString(), evt.AssignedAt, ct);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Analytics audit logged ticket assignment {TicketId} -> {AssignedTo}", evt.TicketId, evt.AssignedToUserId);
    }
}
