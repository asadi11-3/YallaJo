using Accounts.Contracts.IntegrationEvents;
using Accounts.Domain.Events.Agency;
using Accounts.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Infrastructure.EventHandlers;

/// <summary>
/// Converts <see cref="AgencyAffiliationCreatedDomainEvent"/> into outbound integration event.
/// </summary>
internal sealed class PublishAgencyAffiliationCreatedHandler(
    IAccountsOutboxWriter outbox,
    IAgencyAffiliationRepository affiliationRepository,
    ILogger<PublishAgencyAffiliationCreatedHandler> logger)
    : INotificationHandler<DomainEventNotification<AgencyAffiliationCreatedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<AgencyAffiliationCreatedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        // Load the affiliation to get CommissionPercentage (not in domain event for slimness)
        var affiliation = await affiliationRepository.GetByIdAsync(e.AffiliationId, ct);
        var commissionPct = affiliation?.CommissionPercentage ?? 0m;

        var integration = new AgencyAffiliationCreatedIntegrationEvent(
            AffiliationId: e.AffiliationId,
            AgencyUserId: e.AgencyUserId,
            GuideUserId: e.GuideUserId,
            CommissionPercentage: commissionPct,
            CreatedAt: DateTime.UtcNow);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);

        // Also fire the AgencyGuideAffiliated event consumed by Security for role assignment
        var roleEvent = new Accounts.Contracts.IntegrationEvents.AgencyGuideAffiliatedIntegrationEvent(
            UserId: e.GuideUserId,
            AgencyId: e.AgencyUserId,
            AffiliationId: e.AffiliationId);
        await outbox.WriteAsync(roleEvent, ct).ConfigureAwait(false);

        logger.LogInformation("Enqueued accounts.agency.affiliation-created.v1 for Affiliation {Id}", e.AffiliationId);
    }
}

/// <summary>
/// Converts <see cref="AgencyAffiliationTerminatedDomainEvent"/> into outbound integration event.
/// </summary>
internal sealed class PublishAgencyAffiliationTerminatedHandler(
    IAccountsOutboxWriter outbox,
    ILogger<PublishAgencyAffiliationTerminatedHandler> logger)
    : INotificationHandler<DomainEventNotification<AgencyAffiliationTerminatedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<AgencyAffiliationTerminatedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        var integration = new AgencyAffiliationTerminatedIntegrationEvent(
            AffiliationId: e.AffiliationId,
            AgencyUserId: e.AgencyUserId,
            GuideUserId: e.GuideUserId,
            TerminatedByUserId: e.TerminatedByUserId,
            Reason: e.Reason,
            TerminatedAt: DateTime.UtcNow);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);

        logger.LogInformation("Enqueued accounts.agency.affiliation-terminated.v1 for Affiliation {Id}", e.AffiliationId);
    }
}

/// <summary>
/// Converts <see cref="AgencyApplicationApprovedDomainEvent"/> into a notification-only integration event.
/// </summary>
internal sealed class PublishAgencyApplicationApprovedHandler(
    IAccountsOutboxWriter outbox,
    ILogger<PublishAgencyApplicationApprovedHandler> logger)
    : INotificationHandler<DomainEventNotification<AgencyApplicationApprovedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<AgencyApplicationApprovedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        var integration = new AgencyApplicationApprovedIntegrationEvent(
            ApplicationId: e.ApplicationId,
            GuideUserId: e.GuideUserId,
            AgencyUserId: e.AgencyUserId,
            ApprovedAt: DateTime.UtcNow);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);

        logger.LogInformation("Enqueued accounts.agency.application-approved.v1 for Application {Id}", e.ApplicationId);
    }
}

/// <summary>
/// Converts <see cref="AgencyApplicationRejectedDomainEvent"/> into a notification-only integration event.
/// </summary>
internal sealed class PublishAgencyApplicationRejectedHandler(
    IAccountsOutboxWriter outbox,
    ILogger<PublishAgencyApplicationRejectedHandler> logger)
    : INotificationHandler<DomainEventNotification<AgencyApplicationRejectedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<AgencyApplicationRejectedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        var integration = new AgencyApplicationRejectedIntegrationEvent(
            ApplicationId: e.ApplicationId,
            GuideUserId: e.GuideUserId,
            AgencyUserId: e.AgencyUserId,
            Reason: e.Reason,
            RejectedAt: DateTime.UtcNow);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);

        logger.LogInformation("Enqueued accounts.agency.application-rejected.v1 for Application {Id}", e.ApplicationId);
    }
}

/// <summary>
/// Converts <see cref="AgencyInvitationAcceptedDomainEvent"/> into a notification-only integration event.
/// </summary>
internal sealed class PublishAgencyInvitationAcceptedHandler(
    IAccountsOutboxWriter outbox,
    ILogger<PublishAgencyInvitationAcceptedHandler> logger)
    : INotificationHandler<DomainEventNotification<AgencyInvitationAcceptedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<AgencyInvitationAcceptedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        var integration = new AgencyInvitationAcceptedIntegrationEvent(
            InvitationId: e.InvitationId,
            AgencyUserId: e.AgencyUserId,
            GuideUserId: e.GuideUserId,
            AcceptedAt: DateTime.UtcNow);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);

        logger.LogInformation("Enqueued accounts.agency.invitation-accepted.v1 for Invitation {Id}", e.InvitationId);
    }
}

/// <summary>
/// Converts <see cref="AgencyInvitationDeclinedDomainEvent"/> into a notification-only integration event.
/// </summary>
internal sealed class PublishAgencyInvitationDeclinedHandler(
    IAccountsOutboxWriter outbox,
    ILogger<PublishAgencyInvitationDeclinedHandler> logger)
    : INotificationHandler<DomainEventNotification<AgencyInvitationDeclinedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<AgencyInvitationDeclinedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        var integration = new AgencyInvitationDeclinedIntegrationEvent(
            InvitationId: e.InvitationId,
            AgencyUserId: e.AgencyUserId,
            GuideUserId: e.GuideUserId,
            DeclinedAt: DateTime.UtcNow);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);

        logger.LogInformation("Enqueued accounts.agency.invitation-declined.v1 for Invitation {Id}", e.InvitationId);
    }
}
