// <copyright file="FaqItemDeletedDomainEventHandler.cs" company="YallaJo">
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
