using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorPostPublishedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorPostPublishedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorPostPublishedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<CreatorPostPublishedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        var title = await dbContext.CreatorPosts
            .Where(p => p.Id == evt.PostId)
            .Select(p => p.Title)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorPostPublishedIntegrationEvent(
                PostId: evt.PostId,
                CreatorProfileId: evt.CreatorProfileId,
                Title: title,
                IsPreModerated: evt.IsPreModerated,
                PublishedAtUtc: evt.PublishedAtUtc)));

        logger.LogInformation(
            "CreatorPostPublished: staged outbox for post {PostId}, preModerated={IsPreModerated}.",
            evt.PostId,
            evt.IsPreModerated);
    }
}
