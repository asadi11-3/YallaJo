using Accounts.Contracts.IntegrationEvents;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Infrastructure.EventHandlers;

/// <summary>
/// Auto-creates a TourGuide profile when an IndependentGuide provider is approved.
/// Idempotent: skips if a TourGuide record for this user already exists.
/// </summary>
public sealed class ProviderApprovedCreateTourGuideHandler(
    ITourGuideRepository tourGuideRepository,
    IContentToursUnitOfWork unitOfWork,
    IContentToursInboxStore inboxStore,
    ILogger<ProviderApprovedCreateTourGuideHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ProviderApprovedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<ProviderApprovedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var ev = notification.Event;

        // Only act for IndependentGuide approvals.
        if (!string.Equals(ev.ProviderType, "IndependentGuide", StringComparison.Ordinal))
        {
            return;
        }

        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct).ConfigureAwait(false))
        {
            logger.LogDebug(
                "ContentTours: Message {MessageId} (ProviderApproved TourGuide-create for {UserId}) already processed; skipping.",
                notification.MessageId, ev.UserId);
            return;
        }

        // Idempotency: skip if TourGuide already exists for this user.
        var existing = await tourGuideRepository.GetByUserIdAsync(ev.UserId, ct, asNoTracking: true)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            logger.LogInformation(
                "ContentTours: TourGuide profile already exists for UserId={UserId} — skipping creation.",
                ev.UserId);

            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        // Create a minimal TourGuide profile. The guide can flesh out their
        // profile details via the dedicated TourGuide profile endpoint after approval.
        var slug = $"guide-{ev.UserId:N}";
        var displayName = "Independent Guide";
        var guideResult = TourGuide.Register(
            userId: ev.UserId,
            displayName: displayName,
            slug: slug,
            bio: "Independent guide profile — please update via My Profile.",
            yearsOfExperience: 0,
            hasFirstAid: false,
            moTALicenseNumber: null,
            applicationId: ev.ApplicationId);

        if (!guideResult.IsSuccess)
        {
            logger.LogError(
                "ContentTours: Failed to create TourGuide profile for UserId={UserId}: {Error}",
                ev.UserId, guideResult.Error?.Message);
            return;
        }

        var guide = guideResult.Value!;
        await tourGuideRepository.AddAsync(guide, ct).ConfigureAwait(false);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "ContentTours: Created TourGuide profile {GuideId} for newly approved IndependentGuide UserId={UserId}.",
            guide.Id, ev.UserId);
    }
}
