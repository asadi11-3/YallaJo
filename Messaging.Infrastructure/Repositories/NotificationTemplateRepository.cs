using Messaging.Domain.Entities;
using Messaging.Domain.Repositories;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Messaging.Infrastructure.Repositories;

internal sealed class NotificationTemplateRepository(MessagingDbContext context)
    : EfRepository<NotificationTemplate, Guid>(context), INotificationTemplateRepository
{
    public Task<NotificationTemplate?> GetByCodeAsync(string code, CancellationToken ct = default)
        => context.NotificationTemplates.FirstOrDefaultAsync(t => t.Code == code, ct);

    public Task<IReadOnlyList<NotificationTemplate>> GetActiveAsync(CancellationToken ct = default)
        => context.NotificationTemplates
            .Where(t => t.IsActive)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<NotificationTemplate>)t.Result, ct);
}
