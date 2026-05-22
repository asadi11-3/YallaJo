using Messaging.Domain.Entities;
using Messaging.Domain.Repositories;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Messaging.Infrastructure.Repositories;

internal sealed class DeviceTokenRepository(MessagingDbContext context)
    : EfRepository<DeviceToken, Guid>(context), IDeviceTokenRepository
{
    private readonly MessagingDbContext _context = context;

    public async Task<IReadOnlyList<DeviceToken>> GetActiveByUserAsync(Guid userId, CancellationToken ct = default)
        => await _context.DeviceTokens.AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.LastSeenAt)
            .ToListAsync(ct);

    public Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => _context.DeviceTokens.AsNoTracking().FirstOrDefaultAsync(d => d.Token == token, ct);

    public Task<DeviceToken?> GetByUserAndDeviceIdAsync(Guid userId, string deviceId, CancellationToken ct = default)
        => _context.DeviceTokens.FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId, ct);

    public async Task<IReadOnlyList<DeviceToken>> GetStaleAsync(DateTime olderThan, int batchSize, CancellationToken ct = default)
        => await _context.DeviceTokens.AsNoTracking()
            .Where(d => d.LastSeenAt < olderThan)
            .OrderBy(d => d.LastSeenAt)
            .Take(batchSize)
            .ToListAsync(ct);
}
