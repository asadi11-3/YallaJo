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
    private readonly MessagingDbContext _context = context;

    public async Task<IReadOnlyList<NotificationPreference>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _context.NotificationPreferences.AsNoTracking()
            .Where(p => p.UserId == userId).ToListAsync(ct);

    public async Task UpsertAsync(NotificationPreference preference, CancellationToken ct = default)
    {
        var existing = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == preference.UserId
                && p.NotificationType == preference.NotificationType && p.Channel == preference.Channel, ct);
        if (existing is null) _context.NotificationPreferences.Add(preference);
        else existing.SetEnabled(preference.IsEnabled);
    }

    public async Task<bool> IsEnabledForUserAsync(
        Guid userId, NotificationType type, NotificationChannel channel, CancellationToken ct = default)
    {
        var pref = await _context.NotificationPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == type && p.Channel == channel, ct);
        return pref?.IsEnabled ?? true; // default enabled
    }

    public Task<NotificationPreference?> GetByUserAndTypeChannelAsync(Guid userId, NotificationType type, NotificationChannel channel, CancellationToken ct = default)
        => _context.NotificationPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == type && p.Channel == channel, ct)!;
}