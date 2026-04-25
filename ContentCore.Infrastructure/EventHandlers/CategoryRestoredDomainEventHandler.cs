using ContentCore.Contracts.IntegrationEvents;
using ContentCore.Domain.Events;
using ContentCore.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentCore.Infrastructure.EventHandlers;

/// <summary>
/// Handles CategoryRestoredDomainEvent:
/// Publishes <see cref="CategoryRestoredIntegrationEvent"/> to the outbox so downstream
/// modules (e.g. ContentSeo) can re-link orphaned SeoMetadata.
///
/// <para>Does NOT call SaveChangesAsync — the UoW commits atomically after all handlers complete.</para>
/// </summary>
public sealed class CategoryRestoredDomainEventHandler(
    ContentCoreDbContext dbContext,
    ILogger<CategoryRestoredDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CategoryRestoredDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CategoryRestoredDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        logger.LogInformation(
            "CategoryRestoredDomainEvent: queueing outbox for category {CategoryId} (slug: {Slug}).",
            evt.CategoryId, evt.Slug);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CategoryRestoredIntegrationEvent(evt.CategoryId, evt.Slug)));

        return Task.CompletedTask;
    }
}
