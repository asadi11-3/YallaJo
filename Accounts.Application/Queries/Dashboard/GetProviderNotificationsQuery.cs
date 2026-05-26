using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.Dashboard;

/// <summary>Returns recent notifications for the authenticated provider.</summary>
public sealed record GetProviderNotificationsQuery : IQuery<IReadOnlyList<ProviderNotificationDto>>;

public sealed record ProviderNotificationDto(
    string NotificationType,
    string Title,
    string Description,
    string? EntityId,
    DateTime OccurredAt,
    bool IsRead);
