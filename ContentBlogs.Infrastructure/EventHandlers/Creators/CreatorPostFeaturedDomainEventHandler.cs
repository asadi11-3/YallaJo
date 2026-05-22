using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorPostFeaturedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorPostFeaturedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorPostFeaturedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CreatorPostFeaturedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorPostFeaturedIntegrationEvent(
                PostId: evt.PostId,
                CreatorProfileId: evt.CreatorProfileId,
                FeaturedByAdminId: evt.FeaturedByAdminId,
                FeaturedUntilUtc: evt.FeaturedUntilUtc,
                FeaturedAtUtc: DateTime.UtcNow)));

        logger.LogInformation(
            "CreatorPostFeatured: staged outbox for post {PostId}, until {FeaturedUntilUtc}.",
            evt.PostId,
            evt.FeaturedUntilUtc);

        return Task.CompletedTask;
    }
}
