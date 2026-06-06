// <copyright file="SeoMetadataDeletedDomainEventHandler.cs" company="YallaJo">
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
