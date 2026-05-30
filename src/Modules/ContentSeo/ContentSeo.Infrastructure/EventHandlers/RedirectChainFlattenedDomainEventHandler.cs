// <copyright file="RedirectChainFlattenedDomainEventHandler.cs" company="YallaJo">
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
