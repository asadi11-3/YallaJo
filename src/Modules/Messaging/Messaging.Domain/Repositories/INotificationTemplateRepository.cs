using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Messaging.Domain.Repositories;

/// <summary>Repository for NotificationTemplate aggregate queries.</summary>
public interface INotificationTemplateRepository : IRepository<NotificationTemplate, Guid>
{
    Task<NotificationTemplate?> GetByKeyAndLanguageAsync(
        NotificationType type, NotificationChannel channel, string languageCode, CancellationToken ct = default);

    Task<IReadOnlyList<NotificationTemplate>> ListAllAsync(CancellationToken ct = default);
}
