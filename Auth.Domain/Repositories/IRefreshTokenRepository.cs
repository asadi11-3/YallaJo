using Auth.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Auth.Domain.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken, Guid>
{
    /// <summary>
    /// Revokes every currently-active (non-revoked, non-expired) refresh token
    /// for the given user. Mutations are tracked; callers flush via the unit
    /// of work.
    /// <para>
    /// Paired with <see cref="ISessionRepository.RevokeAllActiveForUserAsync"/>
    /// to ensure a credential mutation tears down BOTH the session record and
    /// any rotatable refresh tokens — otherwise an attacker holding a valid
    /// refresh token could obtain a new access token after a password change.
    /// </para>
    /// </summary>
    /// <returns>The number of refresh tokens that were transitioned to revoked.</returns>
    Task<int> RevokeAllActiveForUserAsync(Guid userId, CancellationToken ct = default);
}
