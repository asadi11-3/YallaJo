using Messaging.Domain.Entities;
using Messaging.Domain.Repositories;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Messaging.Infrastructure.Repositories;

internal sealed class DeviceTokenRepository(MessagingDbContext context)
    : EfRepository<DeviceToken, Guid>(context), IDeviceTokenRepository
{
    public Task<IReadOnlyList<DeviceToken>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => context.DeviceTokens
            .Where(d => d.UserId == userId && d.IsActive)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<DeviceToken>)t.Result, ct);

    public Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => context.DeviceTokens.FirstOrDefaultAsync(d => d.Token == token, ct);
}
