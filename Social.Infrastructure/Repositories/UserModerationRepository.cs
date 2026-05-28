using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;

namespace Social.Infrastructure.Repositories;

internal sealed class UserModerationRepository(SocialDbContext context) : IUserModerationRepository
{
    public async Task AddAsync(UserModerationRecord record, CancellationToken ct = default)
        => await context.UserModerationRecords.AddAsync(record, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<UserModerationRecord>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await context.UserModerationRecords
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IssuedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<UserModerationRecord?> GetActiveBanAsync(Guid userId, DateTime now, CancellationToken ct = default)
        => await context.UserModerationRecords
            .Where(x => x.UserId == userId
                && x.Action == ModerationAction.BanUser
                && (x.ExpiresAt == null || x.ExpiresAt > now))
            .OrderByDescending(x => x.IssuedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
}
