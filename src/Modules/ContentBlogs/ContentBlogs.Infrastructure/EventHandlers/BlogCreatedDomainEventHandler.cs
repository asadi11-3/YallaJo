using ContentBlogs.Contracts.IntegrationEvents;
using ContentBlogs.Domain.Events;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogCreatedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<BlogCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogCreatedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BlogCreatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BlogCreatedIntegrationEvent(
                BlogId:       evt.BlogId,
                AuthorId:     evt.AuthorId,
                Slug:         evt.Slug,
                Title:        evt.Title,
                CreatedAtUtc: evt.CreatedAtUtc)));

        logger.LogInformation(
            "BlogCreatedDomainEvent: staged outbox row for blog {BlogId} (Slug={Slug}).",
            evt.BlogId, evt.Slug);

        return Task.CompletedTask;
    }
}
