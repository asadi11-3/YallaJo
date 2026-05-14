// <copyright file="RedirectDomainEventHandlers.cs" company="YallaJo">
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

public sealed class RedirectCreatedDomainEventHandler(
    ContentSeoDbContext dbContext,
    IDateTimeProvider clock,
    ILogger<RedirectCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<RedirectCreatedDomainEvent>>
{
    public Task Handle(DomainEventNotification<RedirectCreatedDomainEvent> notification, CancellationToken ct)
    {
        var evt = notification.Event;
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new RedirectCreatedIntegrationEvent(evt.RedirectId, evt.OldUrl, evt.NewUrl, evt.StatusCode, clock.UtcNow)));
        logger.LogDebug("Staged RedirectCreatedIntegrationEvent for {Id}", evt.RedirectId);
        return Task.CompletedTask;
    }
}

public sealed class RedirectChainFlattenedDomainEventHandler(
    ContentSeoDbContext dbContext,
    IDateTimeProvider clock,
    ILogger<RedirectChainFlattenedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<RedirectChainFlattenedDomainEvent>>
{
    public Task Handle(DomainEventNotification<RedirectChainFlattenedDomainEvent> notification, CancellationToken ct)
    {
        var evt = notification.Event;
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new RedirectChainFlattenedIntegrationEvent(
                evt.RedirectId, evt.OldUrl, evt.OriginalNewUrl, evt.FlattenedNewUrl, clock.UtcNow)));
        logger.LogDebug("Staged RedirectChainFlattenedIntegrationEvent for {Id}", evt.RedirectId);
        return Task.CompletedTask;
    }
}

public sealed class RedirectDeactivatedDomainEventHandler(
    ILogger<RedirectDeactivatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<RedirectDeactivatedDomainEvent>>
{
    public Task Handle(DomainEventNotification<RedirectDeactivatedDomainEvent> notification, CancellationToken ct)
    {
        // No outgoing integration event for deactivation in §3.6 — log only.
        logger.LogDebug("Redirect {Id} deactivated ({OldUrl})", notification.Event.RedirectId, notification.Event.OldUrl);
        return Task.CompletedTask;
    }
}
