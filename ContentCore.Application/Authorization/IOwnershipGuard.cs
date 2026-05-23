using ContentCore.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Authorization;

/// <summary>
/// Centralizes admin-tier bypass + entity ownership checks.
/// Handlers inject this instead of performing manual role/ownership logic.
/// </summary>
public interface IOwnershipGuard
{
    /// <summary>
    /// Returns <see cref="Result.Success()"/> when the current user is admin-tier
    /// or owns the specified entity. Returns a typed failure otherwise.
    /// </summary>
    /// <param name="entityType">The entity type to resolve ownership for.</param>
    /// <param name="entityId">The entity's primary key.</param>
    /// <param name="errorPrefix">
    /// Code prefix for error codes (e.g. "EntityTag", "Attachment").
    /// Produces codes like <c>EntityTag.UnsupportedEntityType</c>.
    /// </param>
    /// <param name="forbiddenMessage">
    /// Optional custom message for the Forbidden error.
    /// Falls back to a generic message when <see langword="null"/>.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> AuthorizeAsync(
        EntityType entityType,
        Guid entityId,
        string errorPrefix,
        string? forbiddenMessage = null,
        CancellationToken ct = default);

    /// <summary>
    /// Whether the current user holds Admin, SuperAdmin, or Owner privileges.
    /// Use sparingly — prefer <see cref="AuthorizeAsync"/> for ownership decisions.
    /// Exposed for edge-case IDOR guards that sit outside the ownership model.
    /// </summary>
    bool IsAdminTier { get; }
}
