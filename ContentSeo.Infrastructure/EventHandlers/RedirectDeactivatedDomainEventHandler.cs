// <copyright file="RedirectDeactivatedDomainEventHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Infrastructure.EventHandlers;

using ContentSeo.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

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
