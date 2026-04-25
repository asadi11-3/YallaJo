using Security.Contracts.Authorization;
using Security.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Authorization;

public interface IRoleHierarchyService
{
    RolePrivilegeLevel GetActingUserLevel();

    Task<RolePrivilegeLevel> GetTargetUserLevelAsync(Guid targetUserId, CancellationToken ct = default);

    Task<Result> EnsureCanManageUserAsync(Guid targetUserId, CancellationToken ct = default);
    Result EnsureCanManageRole(string roleName);

    Result EnsureCanModifyRoleDefinition(string roleName);
}
