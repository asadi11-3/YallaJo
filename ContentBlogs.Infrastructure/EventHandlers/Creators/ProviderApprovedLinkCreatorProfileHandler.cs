using Accounts.Contracts.IntegrationEvents;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

/// <summary>
/// When a provider application is approved (Accounts module), checks if the user
/// also has a creator profile and links them by setting <c>LinkedProviderId</c>.
/// </summary>
public sealed class ProviderApprovedLinkCreatorProfileHandler(
    ICreatorProfileRepository profileRepo,
    IContentBlogsInboxStore inboxStore,
    IContentBlogsUnitOfWork unitOfWork,
    ILogger<ProviderApprovedLinkCreatorProfileHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ProviderApprovedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<ProviderApprovedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentBlogs: Message {MessageId} (ProviderApproved {ApplicationId}) already processed; skipping.",
                notification.MessageId, notification.Event.ApplicationId);
            return;
        }

        var evt = notification.Event;

        var profile = await profileRepo.GetByUserIdAsync(evt.UserId, ct);
        if (profile is not null)
        {
            profile.LinkProvider(evt.ApplicationId);
            logger.LogInformation(
                "ContentBlogs: Linked creator ProfileId={ProfileId} to provider ApplicationId={ApplicationId} for UserId={UserId}",
                profile.Id, evt.ApplicationId, evt.UserId);
        }
        else
        {
            logger.LogDebug(
                "ContentBlogs: No creator profile found for UserId={UserId}; skipping provider link.",
                evt.UserId);
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
