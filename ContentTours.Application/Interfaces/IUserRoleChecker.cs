namespace ContentTours.Application.Interfaces;

public interface IUserRoleChecker
{
    Task<bool> HasRoleAsync(
        Guid userId,
        string roleName,
        CancellationToken cancellationToken);
}
