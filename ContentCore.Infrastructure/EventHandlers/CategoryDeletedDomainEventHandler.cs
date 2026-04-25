using ContentCore.Contracts.IntegrationEvents;
using ContentCore.Domain.Events;
using ContentCore.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentCore.Infrastructure.EventHandlers;

/// <summary>
/// Handles CategoryDeletedDomainEvent:
/// Publishes <see cref="CategoryDeletedIntegrationEvent"/> to the outbox so downstream
/// modules (e.g. ContentSeo) can orphan or remove linked SeoMetadata.
///
/// <para>Does NOT call SaveChangesAsync — the UoW commits atomically after all handlers complete.</para>
/// </summary>
public sealed class CategoryDeletedDomainEventHandler(
    ContentCoreDbContext dbContext,
    ILogger<CategoryDeletedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CategoryDeletedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CategoryDeletedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        logger.LogInformation(
            "CategoryDeletedDomainEvent: queueing outbox for category {CategoryId} (slug: {Slug}).",
            evt.CategoryId, evt.Slug);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CategoryDeletedIntegrationEvent(evt.CategoryId, evt.Slug)));

        return Task.CompletedTask;
    }
}
