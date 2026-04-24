using Auth.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Auth.Domain.Repositories;

public interface ISessionRepository : IRepository<Session, Guid>
{
    /// <summary>
    /// Revokes every currently-active (non-revoked, non-expired) session for the
    /// given user. Mutations are tracked so that <c>Session.Revoke()</c> raises
    /// <c>SessionRevokedEvent</c> for each affected row — callers are expected
    /// to flush via the unit of work (or the ambient transactional executor).
    /// <para>
    /// Used by credential-change flows (self-service password reset, admin
    /// reset, activation, reassignment) where any pre-existing session must
    /// die alongside the credential mutation in the same atomic unit of work.
    /// </para>
    /// </summary>
    /// <returns>The number of sessions that were transitioned to revoked.</returns>
    Task<int> RevokeAllActiveForUserAsync(Guid userId, CancellationToken ct = default);
}
