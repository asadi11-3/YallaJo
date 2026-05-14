// <copyright file="SeoMetadataUpdatedDomainEventHandler.cs" company="YallaJo">
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
