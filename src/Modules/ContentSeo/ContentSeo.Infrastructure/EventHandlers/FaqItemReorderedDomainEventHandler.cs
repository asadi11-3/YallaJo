// <copyright file="FaqItemReorderedDomainEventHandler.cs" company="YallaJo">
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
