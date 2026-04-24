using Auth.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Auth.Domain.Repositories;

/// <summary>
/// Repository for the <see cref="PasswordResetToken"/> aggregate
/// introduced in Phase 2C-2. Inherits the standard read/write surface
/// plus three specialized queries used by the <c>ForgotPassword</c> /
/// <c>ResetPassword</c> pipelines.
/// </summary>
public interface IPasswordResetTokenRepository
    : IWriteRepository<PasswordResetToken, Guid>,
      IReadRepository<PasswordResetToken, Guid>
{
    /// <summary>
    /// Returns every non-terminal reset token (state <c>Issued</c> or
    /// <c>Delivered</c>) for the given user, loaded as tracked entities
    /// so the caller can invoke <see cref="PasswordResetToken.Supersede"/>
    /// on each and flush via the unit of work.
    /// </summary>
    Task<IReadOnlyList<PasswordResetToken>> GetActiveForUserAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the most recently issued non-terminal token for the
    /// given user (state <c>Issued</c> or <c>Delivered</c>), loaded as
    /// a tracked entity so the caller can invoke
    /// <see cref="PasswordResetToken.Consume"/> or
    /// <see cref="PasswordResetToken.IncrementAttempt"/> and flush via
    /// the unit of work.
    /// </summary>
    Task<PasswordResetToken?> GetLatestActiveForUserAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the most recently issued non-terminal token for the
    /// given user, loaded AsNoTracking for read-only throttle checks
    /// (the handler only inspects <c>IssuedAt</c> / <c>CreatedAt</c>
    /// and must not drag a tracked entity into the subsequent
    /// supersede sweep).
    /// </summary>
    Task<PasswordResetToken?> GetLatestActiveForUserReadOnlyAsync(
        Guid userId,
        CancellationToken ct = default);
}
