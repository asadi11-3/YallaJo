namespace ContentTours.Application.Interfaces;

/// <summary>
/// Cross-module contract for verifying that a user holds a particular
/// security role. The canonical implementation will live in the Security
/// module; ContentTours uses a NoOp stub until that integration lands
/// (per Phase C deferral).
/// </summary>
/// <remarks>
/// Used by AssignTourGuideCommandHandler to validate that the target
/// user has the "TourGuide" role before being assigned to a tour
/// (PDF B5 domain rule). This is distinct from the caller's authorization
/// gate (AppRoles.HighestPrivilegeLevel(currentUser.Roles)), which checks
/// whether the API caller can perform the action. This check verifies
/// the target user's domain role.
/// </remarks>
public interface IUserRoleChecker
{
    Task<bool> HasRoleAsync(
        Guid userId,
        string roleName,
        CancellationToken cancellationToken);
}
