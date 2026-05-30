using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorInvitationSentDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorInvitationSentDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorInvitationSentDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CreatorInvitationSentDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorInvitationSentIntegrationEvent(
                InvitationId:  evt.InvitationId,
                Kind:          evt.Kind.ToString(),
                Email:         evt.Email,
                InvitedUserId: evt.InvitedUserId,
                SentByAdminId: evt.SentByAdminId,
                ExpiresAtUtc:  DateTime.UtcNow.AddDays(CreatorInvitation.ExpiryDays))));

        logger.LogInformation(
            "CreatorInvitationSent: staged outbox for invitation {InvitationId}.",
            evt.InvitationId);

        return Task.CompletedTask;
    }
}
