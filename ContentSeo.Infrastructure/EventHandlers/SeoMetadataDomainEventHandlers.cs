// <copyright file="SeoMetadataDomainEventHandlers.cs" company="YallaJo">
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

public sealed class SeoMetadataCreatedDomainEventHandler(
    ContentSeoDbContext dbContext,
    IDateTimeProvider clock,
    ILogger<SeoMetadataCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<SeoMetadataCreatedDomainEvent>>
{
    public Task Handle(DomainEventNotification<SeoMetadataCreatedDomainEvent> notification, CancellationToken ct)
    {
        var evt = notification.Event;
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new SeoMetadataChangedIntegrationEvent(evt.SeoMetadataId, evt.EntityType, evt.EntityId, clock.UtcNow)));
        logger.LogDebug("Staged SeoMetadataChangedIntegrationEvent (Created) for {Id}", evt.SeoMetadataId);
        return Task.CompletedTask;
    }
}

public sealed class SeoMetadataUpdatedDomainEventHandler(
    ContentSeoDbContext dbContext,
    IDateTimeProvider clock,
    ILogger<SeoMetadataUpdatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<SeoMetadataUpdatedDomainEvent>>
{
    public Task Handle(DomainEventNotification<SeoMetadataUpdatedDomainEvent> notification, CancellationToken ct)
    {
        var evt = notification.Event;
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new SeoMetadataChangedIntegrationEvent(evt.SeoMetadataId, evt.EntityType, evt.EntityId, clock.UtcNow)));
        logger.LogDebug("Staged SeoMetadataChangedIntegrationEvent (Updated) for {Id}", evt.SeoMetadataId);
        return Task.CompletedTask;
    }
}

public sealed class SeoMetadataDeletedDomainEventHandler(
    ContentSeoDbContext dbContext,
    IDateTimeProvider clock,
    ILogger<SeoMetadataDeletedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<SeoMetadataDeletedDomainEvent>>
{
    public Task Handle(DomainEventNotification<SeoMetadataDeletedDomainEvent> notification, CancellationToken ct)
    {
        var evt = notification.Event;
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new SeoMetadataChangedIntegrationEvent(evt.SeoMetadataId, evt.EntityType, evt.EntityId, clock.UtcNow)));
        logger.LogDebug("Staged SeoMetadataChangedIntegrationEvent (Deleted) for {Id}", evt.SeoMetadataId);
        return Task.CompletedTask;
    }
}
