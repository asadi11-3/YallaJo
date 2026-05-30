using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Auth.Infrastructure.Repositories;

/// <summary>
/// Phase 2C-1 — EF-backed repository for <see cref="ActivationToken"/>.
/// Inherits the shared-kernel generic surface and adds the two targeted
/// queries the activation pipeline needs.
/// </summary>
public sealed class ActivationTokenRepository(AuthDbContext context)
    : EfEntityRepository<ActivationToken, Guid>(context), IActivationTokenRepository
{
    private readonly AuthDbContext _context = context;

    public async Task<IReadOnlyList<ActivationToken>> GetActiveForUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        // Tracked load — callers invoke Supersede()/IncrementAttempt() on
        // the returned entities and flush via the unit of work.
        return await _context.Set<ActivationToken>()
            .Where(t => t.UserId == userId
                     && (t.State == ActivationTokenState.Issued
                      || t.State == ActivationTokenState.Delivered))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ActivationToken?> GetLatestActiveForUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        return await _context.Set<ActivationToken>()
            .Where(t => t.UserId == userId
                     && (t.State == ActivationTokenState.Issued
                      || t.State == ActivationTokenState.Delivered))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }
}
