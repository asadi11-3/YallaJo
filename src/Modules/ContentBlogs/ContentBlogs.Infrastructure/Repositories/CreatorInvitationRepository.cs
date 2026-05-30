using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentBlogs.Infrastructure.Repositories;

public class CreatorInvitationRepository(ContentBlogsDbContext context)
    : EfRepository<CreatorInvitation, Guid>(context), ICreatorInvitationRepository
{
    public Task<CreatorInvitation?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorInvitations
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);
    }

    public async Task<List<CreatorInvitation>> GetExpiredPendingAsync(
        DateTime utcNow,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await context.CreatorInvitations
            .Where(i => i.Status == CreatorInvitationStatus.Pending && i.ExpiresAt <= utcNow)
            .OrderBy(i => i.ExpiresAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> HasPendingInvitationForEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return context.CreatorInvitations
            .AnyAsync(
                i => i.Status == CreatorInvitationStatus.Pending &&
                     i.Email != null &&
                     i.Email.ToLower() == normalizedEmail,
                cancellationToken);
    }

    public Task<bool> HasPendingInvitationForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorInvitations
            .AnyAsync(
                i => i.Status == CreatorInvitationStatus.Pending &&
                     i.InvitedUserId == userId,
                cancellationToken);
    }
}
