using ContentBlogs.Contracts.IntegrationEvents;
using ContentBlogs.Domain.Events;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogUpdatedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<BlogUpdatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogUpdatedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BlogUpdatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BlogUpdatedIntegrationEvent(
                BlogId:         evt.BlogId,
                OldSlug:        evt.OldSlug,
                NewSlug:        evt.NewSlug,
                FieldsChanged:  evt.FieldsChanged,
                UpdatedAt:      evt.UpdatedAtUtc)));

        logger.LogInformation(
            "BlogUpdatedDomainEvent: staged outbox row for blog {BlogId} (OldSlug={OldSlug}, NewSlug={NewSlug}).",
            evt.BlogId, evt.OldSlug, evt.NewSlug);

        return Task.CompletedTask;
    }
}
