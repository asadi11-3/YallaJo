// <copyright file="FaqItemDomainEventHandlers.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Infrastructure.EventHandlers;

using ContentSeo.Contracts.IntegrationEvents;
using ContentSeo.Domain.Events;
using ContentSeo.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Clock;
using YallaJo.SharedKernel.Infrastructure.Outbox;

public sealed class FaqItemCreatedDomainEventHandler(
    ContentSeoDbContext dbContext,
    IDateTimeProvider clock,
    ILogger<FaqItemCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<FaqItemCreatedDomainEvent>>
{
    public Task Handle(DomainEventNotification<FaqItemCreatedDomainEvent> notification, CancellationToken ct)
    {
        var evt = notification.Event;
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new FaqItemChangedIntegrationEvent(evt.FaqItemId, evt.EntityType, evt.EntityId, "Created", clock.UtcNow)));
        logger.LogDebug("Staged FaqItemChangedIntegrationEvent (Created) for {Id}", evt.FaqItemId);
        return Task.CompletedTask;
    }
}

public sealed class FaqItemUpdatedDomainEventHandler(
    ContentSeoDbContext dbContext,
    IDateTimeProvider clock,
    ILogger<FaqItemUpdatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<FaqItemUpdatedDomainEvent>>
{
    public Task Handle(DomainEventNotification<FaqItemUpdatedDomainEvent> notification, CancellationToken ct)
    {
        var evt = notification.Event;
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new FaqItemChangedIntegrationEvent(evt.FaqItemId, evt.EntityType, evt.EntityId, "Updated", clock.UtcNow)));
        logger.LogDebug("Staged FaqItemChangedIntegrationEvent (Updated) for {Id}", evt.FaqItemId);
        return Task.CompletedTask;
    }
}

public sealed class FaqItemDeletedDomainEventHandler(
    ContentSeoDbContext dbContext,
    IDateTimeProvider clock,
    ILogger<FaqItemDeletedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<FaqItemDeletedDomainEvent>>
{
    public Task Handle(DomainEventNotification<FaqItemDeletedDomainEvent> notification, CancellationToken ct)
    {
        var evt = notification.Event;
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new FaqItemChangedIntegrationEvent(evt.FaqItemId, evt.EntityType, evt.EntityId, "Deleted", clock.UtcNow)));
        logger.LogDebug("Staged FaqItemChangedIntegrationEvent (Deleted) for {Id}", evt.FaqItemId);
        return Task.CompletedTask;
    }
}

public sealed class FaqItemReorderedDomainEventHandler(
    ContentSeoDbContext dbContext,
    IDateTimeProvider clock,
    ILogger<FaqItemReorderedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<FaqItemReorderedDomainEvent>>
{
    public Task Handle(DomainEventNotification<FaqItemReorderedDomainEvent> notification, CancellationToken ct)
    {
        var evt = notification.Event;
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new FaqItemChangedIntegrationEvent(evt.FaqItemId, evt.EntityType, evt.EntityId, "Reordered", clock.UtcNow)));
        logger.LogDebug("Staged FaqItemChangedIntegrationEvent (Reordered) for {Id}", evt.FaqItemId);
        return Task.CompletedTask;
    }
}
