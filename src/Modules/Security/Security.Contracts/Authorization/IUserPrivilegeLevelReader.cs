namespace Security.Contracts.Authorization;

public interface IUserPrivilegeLevelReader
{
    Task<RolePrivilegeLevel> GetPrivilegeLevelAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
