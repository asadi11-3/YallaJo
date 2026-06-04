using Accounts.Contracts.IntegrationEvents;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Infrastructure.Persistence;
using ContentTours.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Infrastructure.EventHandlers;

/// <summary>
/// Provisions a Booking-side <see cref="TourGuide"/> registry row for a user when a
/// guide/provider is activated upstream. This is what lets a newly-approved provider
/// own availability slots (slot guide is resolved from this registry by UserId).
/// <para>
/// Idempotent: if an active row already exists it is a no-op; a previously
/// deactivated row is reactivated; otherwise a new active row is created.
/// </para>
/// <para>
/// Handles two triggers:
/// <list type="bullet">
///   <item><see cref="TourGuideActivatedIntegrationEvent"/> — a guide becomes active.</item>
///   <item><see cref="ProviderApprovedIntegrationEvent"/> — fallback for provider owners
///   who operate tours but were not separately activated as a guide.</item>
/// </list>
/// </para>
/// </summary>
public sealed class GuideActivatedProvisionHandler(
    BookingDbContext dbContext,
    IBookingUnitOfWork unitOfWork,
    ILogger<GuideActivatedProvisionHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourGuideActivatedIntegrationEvent>>,
      INotificationHandler<IntegrationEventNotification<ProviderApprovedIntegrationEvent>>
{
    public Task Handle(
        IntegrationEventNotification<TourGuideActivatedIntegrationEvent> notification,
        CancellationToken ct)
        => EnsureGuideAsync(notification.Event.UserId, "TourGuideActivated", ct);

    public Task Handle(
        IntegrationEventNotification<ProviderApprovedIntegrationEvent> notification,
        CancellationToken ct)
        => EnsureGuideAsync(notification.Event.UserId, "ProviderApproved", ct);

    private async Task EnsureGuideAsync(Guid userId, string trigger, CancellationToken ct)
    {
        if (userId == Guid.Empty)
        {
            return;
        }

        var existing = await dbContext.TourGuides
            .FirstOrDefaultAsync(g => g.UserId == userId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            if (existing.IsActive)
            {
                return; // already provisioned — no-op
            }

            existing.Activate();
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            logger.LogInformation(
                "Booking: reactivated TourGuide registry row for UserId={UserId} (trigger={Trigger}).",
                userId, trigger);
            return;
        }

        dbContext.TourGuides.Add(TourGuide.Create(userId));
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Booking: provisioned TourGuide registry row for UserId={UserId} (trigger={Trigger}).",
            userId, trigger);
    }
}
