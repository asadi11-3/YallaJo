using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Auth.Infrastructure.Repositories;

public sealed class RefreshTokenRepository(AuthDbContext context)
    : EfRepository<RefreshToken, Guid>(context), IRefreshTokenRepository
{
    private readonly AuthDbContext _context = context;

    public async Task<int> RevokeAllActiveForUserAsync(Guid userId, CancellationToken ct = default)
    {
        // Tracked mutation — lets RefreshToken.Revoke() participate in the
        // same unit of work as the caller's credential change, ensuring
        // atomic commit with password/session state transitions.
        var now = DateTime.UtcNow;
        var active = await _context.Set<RefreshToken>()
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > now)
            .ToListAsync(ct);

        foreach (var token in active)
            token.Revoke();

        return active.Count;
    }
}
