using Auth.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Auth.Domain.Repositories;

/// <summary>
/// Repository for the <see cref="ActivationToken"/> aggregate introduced in
/// Phase 2C-1. Inherits the standard read/write surface plus two
/// specialized queries used by the <c>SendActivationEmail</c> /
/// <c>ActivateAccount</c> pipelines.
/// </summary>
public interface IActivationTokenRepository
    : IWriteRepository<ActivationToken, Guid>,
      IReadRepository<ActivationToken, Guid>
{
    /// <summary>
    /// Returns every non-terminal activation token (state
    /// <c>Issued</c> or <c>Delivered</c>) for the given user, loaded as
    /// tracked entities so the caller can invoke
    /// <see cref="ActivationToken.Supersede"/> on each and flush via the
    /// unit of work.
    /// </summary>
    Task<IReadOnlyList<ActivationToken>> GetActiveForUserAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the most recently issued non-terminal token for the given
    /// user (state <c>Issued</c> or <c>Delivered</c>), loaded as a tracked
    /// entity so the caller can invoke <see cref="ActivationToken.Consume"/>
    /// or <see cref="ActivationToken.IncrementAttempt"/> and flush via the
    /// unit of work.
    /// </summary>
    Task<ActivationToken?> GetLatestActiveForUserAsync(
        Guid userId,
        CancellationToken ct = default);
}
