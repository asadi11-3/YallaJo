using Accounts.Contracts.IntegrationEvents;
using Accounts.Domain.Events;
using Accounts.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Infrastructure.EventHandlers;

/// <summary>
/// Converts <see cref="ProviderRegisteredDomainEvent"/> into the outbound integration event.
/// </summary>
internal sealed class PublishProviderRegisteredHandler(
    IAccountsOutboxWriter outbox,
    ILogger<PublishProviderRegisteredHandler> logger)
    : INotificationHandler<DomainEventNotification<ProviderRegisteredDomainEvent>>
{
    public async Task Handle(DomainEventNotification<ProviderRegisteredDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new ProviderRegisteredIntegrationEvent(
            ApplicationId: e.ApplicationId,
            UserId: e.UserId,
            ProviderType: e.Type.ToString(),
            RegisteredAt: e.RegisteredAt);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);

        var statusChanged = new ProviderStatusChangedIntegrationEvent(
            ApplicationId: e.ApplicationId,
            UserId: e.UserId,
            PreviousStatus: "None",
            NewStatus: "Draft",
            ChangedAt: e.RegisteredAt);
        await outbox.WriteAsync(statusChanged, ct).ConfigureAwait(false);

        logger.LogInformation("Enqueued accounts.provider.registered.v1 for Application {Id}", e.ApplicationId);
    }
}

/// <summary>
/// Converts <see cref="ProviderApprovedDomainEvent"/> into the outbound integration event.
/// </summary>
internal sealed class PublishProviderApprovedHandler(
    IAccountsOutboxWriter outbox,
    ILogger<PublishProviderApprovedHandler> logger)
    : INotificationHandler<DomainEventNotification<ProviderApprovedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<ProviderApprovedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new ProviderApprovedIntegrationEvent(
            ApplicationId: e.ApplicationId,
            UserId: e.UserId,
            ProviderType: e.Type.ToString(),
            ApprovedAt: e.ApprovedAt,
            ApprovedByAdminId: e.ApprovedByAdminId);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);

        var statusChanged = new ProviderStatusChangedIntegrationEvent(
            ApplicationId: e.ApplicationId,
            UserId: e.UserId,
            PreviousStatus: "Pending",
            NewStatus: "Approved",
            ChangedAt: e.ApprovedAt);
        await outbox.WriteAsync(statusChanged, ct).ConfigureAwait(false);

        logger.LogInformation("Enqueued accounts.provider.approved.v1 for Application {Id}", e.ApplicationId);
    }
}

/// <summary>
/// Converts <see cref="ProviderRejectedDomainEvent"/> into the outbound integration event.
/// </summary>
internal sealed class PublishProviderRejectedHandler(
    IAccountsOutboxWriter outbox,
    ILogger<PublishProviderRejectedHandler> logger)
    : INotificationHandler<DomainEventNotification<ProviderRejectedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<ProviderRejectedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new ProviderRejectedIntegrationEvent(
            ApplicationId: e.ApplicationId,
            UserId: e.UserId,
            Reason: e.Reason,
            RejectedAt: e.RejectedAt);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);

        var statusChanged = new ProviderStatusChangedIntegrationEvent(
            ApplicationId: e.ApplicationId,
            UserId: e.UserId,
            PreviousStatus: "Pending",
            NewStatus: "Rejected",
            ChangedAt: e.RejectedAt);
        await outbox.WriteAsync(statusChanged, ct).ConfigureAwait(false);

        logger.LogInformation("Enqueued accounts.provider.rejected.v1 for Application {Id}", e.ApplicationId);
    }
}

/// <summary>
/// Converts <see cref="ProviderSuspendedDomainEvent"/> into the outbound integration event.
/// </summary>
internal sealed class PublishProviderSuspendedHandler(
    IAccountsOutboxWriter outbox,
    ILogger<PublishProviderSuspendedHandler> logger)
    : INotificationHandler<DomainEventNotification<ProviderSuspendedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<ProviderSuspendedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new ProviderSuspendedIntegrationEvent(
            ApplicationId: e.ApplicationId,
            UserId: e.UserId,
            Reason: e.Reason,
            SuspendedAt: e.SuspendedAt);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);

        var statusChanged = new ProviderStatusChangedIntegrationEvent(
            ApplicationId: e.ApplicationId,
            UserId: e.UserId,
            PreviousStatus: "Approved",
            NewStatus: "Suspended",
            ChangedAt: e.SuspendedAt);
        await outbox.WriteAsync(statusChanged, ct).ConfigureAwait(false);

        logger.LogInformation("Enqueued accounts.provider.suspended.v1 for Application {Id}", e.ApplicationId);
    }
}

/// <summary>
/// Converts <see cref="ProviderReinstatedDomainEvent"/> into the outbound integration event.
/// </summary>
internal sealed class PublishProviderReinstatedHandler(
    IAccountsOutboxWriter outbox,
    ILogger<PublishProviderReinstatedHandler> logger)
    : INotificationHandler<DomainEventNotification<ProviderReinstatedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<ProviderReinstatedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new ProviderReinstatedIntegrationEvent(
            ApplicationId: e.ApplicationId,
            UserId: e.UserId,
            ReinstatedAt: e.ReinstatedAt);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);

        var statusChanged = new ProviderStatusChangedIntegrationEvent(
            ApplicationId: e.ApplicationId,
            UserId: e.UserId,
            PreviousStatus: "Suspended",
            NewStatus: "Approved",
            ChangedAt: e.ReinstatedAt);
        await outbox.WriteAsync(statusChanged, ct).ConfigureAwait(false);

        logger.LogInformation("Enqueued accounts.provider.reinstated.v1 for Application {Id}", e.ApplicationId);
    }
}

/// <summary>
/// Converts <see cref="ProviderMoreDocsRequestedDomainEvent"/> into status-changed integration event.
/// </summary>
internal sealed class PublishProviderMoreDocsRequestedHandler(
    IAccountsOutboxWriter outbox,
    ILogger<PublishProviderMoreDocsRequestedHandler> logger)
    : INotificationHandler<DomainEventNotification<ProviderMoreDocsRequestedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<ProviderMoreDocsRequestedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        var statusChanged = new ProviderStatusChangedIntegrationEvent(
            ApplicationId: e.ApplicationId,
            UserId: e.UserId,
            PreviousStatus: "Pending",
            NewStatus: "MoreDocsNeeded",
            ChangedAt: e.RequestedAt);
        await outbox.WriteAsync(statusChanged, ct).ConfigureAwait(false);

        logger.LogInformation("Enqueued accounts.provider.status-changed.v1 (MoreDocsNeeded) for Application {Id}", e.ApplicationId);
    }
}

/// <summary>
/// Converts <see cref="ProviderApplicationSubmittedDomainEvent"/> into status-changed integration event.
/// </summary>
internal sealed class PublishProviderApplicationSubmittedHandler(
    IAccountsOutboxWriter outbox,
    ILogger<PublishProviderApplicationSubmittedHandler> logger)
    : INotificationHandler<DomainEventNotification<ProviderApplicationSubmittedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<ProviderApplicationSubmittedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        var statusChanged = new ProviderStatusChangedIntegrationEvent(
            ApplicationId: e.ApplicationId,
            UserId: e.UserId,
            PreviousStatus: "Draft",
            NewStatus: "Pending",
            ChangedAt: e.SubmittedAt);
        await outbox.WriteAsync(statusChanged, ct).ConfigureAwait(false);

        logger.LogInformation("Enqueued accounts.provider.status-changed.v1 (Pending) for Application {Id}", e.ApplicationId);
    }
}
