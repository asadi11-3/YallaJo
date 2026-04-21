using Security.Contracts.Authorization;
using Security.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Authorization;

/// <summary>
/// Centralized enforcement of the role-management privilege hierarchy.
///
/// Hierarchy (highest first):
///   Owner &gt; SuperAdmin &gt; Admin &gt; Standard (User / TourGuide / Guest).
///
/// Rules enforced:
///   1. Owner may manage anyone.
///   2. SuperAdmin may manage Admin + Standard. NOT another SuperAdmin. NOT Owner.
///   3. Admin may manage Standard only. NOT another Admin. NOT SuperAdmin. NOT Owner.
///   4. Standard roles may not manage privileged accounts.
///   5. Same-level management is forbidden (unless actor is Owner — Owner is singleton anyway).
///   6. When assigning or removing a role, the actor must strictly outrank the role itself.
///   7. Actors may not demote/promote their own privileged role.
/// </summary>
public interface IRoleHierarchyService
{
    /// <summary>
    /// Resolves the privilege level of the currently authenticated user
    /// from <see cref="YallaJo.SharedKernel.Application.Abstractions.Context.ICurrentUser.Roles"/>.
    /// </summary>
    RolePrivilegeLevel GetActingUserLevel();

    /// <summary>
    /// Resolves the privilege level of a target user from their persisted <see cref="UserRole"/>s.
    /// </summary>
    Task<RolePrivilegeLevel> GetTargetUserLevelAsync(Guid targetUserId, CancellationToken ct = default);

    /// <summary>
    /// Ensures the currently authenticated user is allowed to manage the given target user.
    /// Returns <see cref="Result.Success()"/> on allow, or a failure Result on deny.
    /// </summary>
    /// <remarks>
    /// Callers are expected to have already verified that the target user exists.
    /// This method does NOT perform the existence check — it only performs hierarchy comparison.
    /// </remarks>
    Task<Result> EnsureCanManageUserAsync(Guid targetUserId, CancellationToken ct = default);

    /// <summary>
    /// Ensures the currently authenticated user is allowed to assign or remove the specified role.
    /// Must be used in addition to <see cref="EnsureCanManageUserAsync"/> for role-assignment flows.
    /// </summary>
    Result EnsureCanManageRole(string roleName);

    /// <summary>
    /// Ensures the currently authenticated user is allowed to modify a role definition
    /// (update description, deactivate, add/remove role claims). The actor must strictly
    /// outrank the role being modified. Unknown/standard roles are treated as Standard.
    /// </summary>
    Result EnsureCanModifyRoleDefinition(string roleName);
}
