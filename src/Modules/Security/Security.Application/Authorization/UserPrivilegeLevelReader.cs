using Security.Contracts.Authorization;

namespace Security.Application.Authorization;

internal sealed class UserPrivilegeLevelReader(IRoleHierarchyService roleHierarchyService)
    : IUserPrivilegeLevelReader
{
    public Task<RolePrivilegeLevel> GetPrivilegeLevelAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => roleHierarchyService.GetTargetUserLevelAsync(userId, cancellationToken);
}
