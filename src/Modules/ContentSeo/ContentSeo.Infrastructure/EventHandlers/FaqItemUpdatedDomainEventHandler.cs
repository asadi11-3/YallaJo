// <copyright file="FaqItemUpdatedDomainEventHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Infrastructure.EventHandlers;

using ContentSeo.Contracts.IntegrationEvents;
using ContentSeo.Domain.Events;
using ContentSeo.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Clock;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

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
