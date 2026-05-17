using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Domain.Repositories;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Messaging.Infrastructure.Repositories;

internal sealed class NotificationPreferenceRepository(MessagingDbContext context)
    : EfRepository<NotificationPreference, Guid>(context), INotificationPreferenceRepository
{
    public Task<IReadOnlyList<NotificationPreference>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => context.NotificationPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<NotificationPreference>)t.Result, ct);

    public Task<NotificationPreference?> GetByUserAndTypeAsync(Guid userId, NotificationType type, NotificationChannel channel, CancellationToken ct = default)
        => context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == type && p.Channel == channel, ct);
}
