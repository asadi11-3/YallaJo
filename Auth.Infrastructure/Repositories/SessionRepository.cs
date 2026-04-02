using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Auth.Infrastructure.Repositories;

public sealed class SessionRepository(AuthDbContext context)
    : EfRepository<Session, Guid>(context), ISessionRepository
{
    public Task<List<Session>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return context.Set<Session>()
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now)
            .OrderByDescending(s => s.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
