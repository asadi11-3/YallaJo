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
