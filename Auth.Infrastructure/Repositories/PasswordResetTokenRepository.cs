using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Auth.Infrastructure.Repositories;

/// <summary>
/// Phase 2C-2 — EF-backed repository for <see cref="PasswordResetToken"/>.
/// Inherits the shared-kernel generic surface and adds the three
/// targeted queries the reset pipeline needs.
/// </summary>
public sealed class PasswordResetTokenRepository(AuthDbContext context)
    : EfEntityRepository<PasswordResetToken, Guid>(context), IPasswordResetTokenRepository
{
    private readonly AuthDbContext _context = context;

    public async Task<IReadOnlyList<PasswordResetToken>> GetActiveForUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        // Tracked load — callers invoke Supersede()/IncrementAttempt() on
        // the returned entities and flush via the unit of work.
        return await _context.Set<PasswordResetToken>()
            .Where(t => t.UserId == userId
                     && (t.State == PasswordResetTokenState.Issued
                      || t.State == PasswordResetTokenState.Delivered))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<PasswordResetToken?> GetLatestActiveForUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        return await _context.Set<PasswordResetToken>()
            .Where(t => t.UserId == userId
                     && (t.State == PasswordResetTokenState.Issued
                      || t.State == PasswordResetTokenState.Delivered))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PasswordResetToken?> GetLatestActiveForUserReadOnlyAsync(
        Guid userId, CancellationToken ct = default)
    {
        return await _context.Set<PasswordResetToken>()
            .AsNoTracking()
            .Where(t => t.UserId == userId
                     && (t.State == PasswordResetTokenState.Issued
                      || t.State == PasswordResetTokenState.Delivered))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }
}
