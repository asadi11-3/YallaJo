using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Domain.Repositories;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Messaging.Infrastructure.Repositories;

internal sealed class NotificationTemplateRepository(MessagingDbContext context)
    : EfRepository<NotificationTemplate, Guid>(context), INotificationTemplateRepository
{
    private readonly MessagingDbContext _context = context;

    public Task<NotificationTemplate?> GetByKeyAndLanguageAsync(
        NotificationType type, NotificationChannel channel, string languageCode, CancellationToken ct = default)
        => _context.NotificationTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Type == type && t.Channel == channel && t.LanguageCode == languageCode, ct);

    public async Task<IReadOnlyList<NotificationTemplate>> ListAllAsync(CancellationToken ct = default)
        => await _context.NotificationTemplates.AsNoTracking()
            .OrderBy(t => t.Type).ThenBy(t => t.Channel).ThenBy(t => t.LanguageCode)
            .ToListAsync(ct);
}
