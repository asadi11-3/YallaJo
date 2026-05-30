using ContentBlogs.Domain.Entities.Creators;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentBlogs.Domain.Repositories;

public interface ICreatorInvitationRepository : IRepository<CreatorInvitation, Guid>
{
    /// <summary>
    /// Finds a pending invitation by its unique token.
    /// Used during the invitation redemption flow.
    /// </summary>
    Task<CreatorInvitation?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all pending invitations whose expiry date has passed.
    /// Used by <see cref="Infrastructure.BackgroundServices.CreatorInvitationCleanupService"/>.
    /// </summary>
    Task<List<CreatorInvitation>> GetExpiredPendingAsync(
        DateTime utcNow,
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether there is already a pending invitation for the given email address.
    /// </summary>
    Task<bool> HasPendingInvitationForEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether there is already a pending in-app invitation for the given user.
    /// </summary>
    Task<bool> HasPendingInvitationForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
