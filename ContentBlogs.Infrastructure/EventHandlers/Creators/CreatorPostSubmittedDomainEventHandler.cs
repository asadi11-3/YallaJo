using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorPostSubmittedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorPostSubmittedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorPostSubmittedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<CreatorPostSubmittedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        var title = await dbContext.CreatorPosts
            .Where(p => p.Id == evt.PostId)
            .Select(p => p.Title)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorPostSubmittedForReviewIntegrationEvent(
                PostId: evt.PostId,
                CreatorProfileId: evt.CreatorProfileId,
                Title: title,
                SubmittedAtUtc: evt.SubmittedAtUtc)));

        logger.LogInformation(
            "CreatorPostSubmitted: staged outbox for post {PostId}.",
            evt.PostId);
    }
}
