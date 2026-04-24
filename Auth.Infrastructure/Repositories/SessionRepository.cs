using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Auth.Infrastructure.Repositories;

public sealed class SessionRepository(AuthDbContext context)
    : EfRepository<Session, Guid>(context), ISessionRepository
{
    private readonly AuthDbContext _context = context;

    public async Task<int> RevokeAllActiveForUserAsync(Guid userId, CancellationToken ct = default)
    {
        // Load as tracked entities so Session.Revoke() raises SessionRevokedEvent
        // for each row. Dispatch happens at the next SaveChangesAsync on the
        // AuthDbContext (via UnitOfWork). Bounded set in practice (handful of
        // active sessions per user), so loading-then-mutating is safe.
        var now = DateTime.UtcNow;
        var active = await _context.Set<Session>()
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now)
            .ToListAsync(ct);

        foreach (var session in active)
            session.Revoke();

        return active.Count;
    }
}
