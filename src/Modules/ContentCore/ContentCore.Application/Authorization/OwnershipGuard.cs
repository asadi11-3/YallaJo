using ContentCore.Domain.Enums;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Authorization;

/// <inheritdoc />
public sealed class OwnershipGuard(
    ICurrentUser currentUser,
    IEntityOwnershipResolver ownershipResolver) : IOwnershipGuard
{
    /// <inheritdoc />
    public bool IsAdminTier =>
        AppRoles.HighestPrivilegeLevel(currentUser.Roles) >= RolePrivilegeLevel.Admin;

    /// <inheritdoc />
    public async Task<Result> AuthorizeAsync(
        EntityType entityType,
        Guid entityId,
        string errorPrefix,
        string? forbiddenMessage = null,
        CancellationToken ct = default)
    {
        if (IsAdminTier)
            return Result.Success();

        var ownership = await ownershipResolver.ResolveAsync(entityType, entityId, ct);

        if (!ownership.IsSupported)
        {
            return Result.Failure(
                new Error(
                    $"{errorPrefix}.UnsupportedEntityType",
                    "This entity type cannot be authorized for this operation."),
                Outcome.Invalid);
        }

        if (!ownership.Exists)
        {
            return Result.Failure(
                new Error(
                    $"{errorPrefix}.TargetNotFound",
                    $"{entityType} '{entityId}' was not found."),
                Outcome.NotFound);
        }

        if (ownership.IsDeleted)
        {
            return Result.Failure(
                new Error(
                    $"{errorPrefix}.TargetDeleted",
                    $"{entityType} '{entityId}' is deleted."),
                Outcome.Invalid);
        }

        if (ownership.OwnerUserId != currentUser.UserId!.Value)
        {
            return Result.Failure(
                Error.Forbidden(forbiddenMessage ?? "You do not have permission to perform this operation."),
                Outcome.Forbidden);
        }

        return Result.Success();
    }
}
