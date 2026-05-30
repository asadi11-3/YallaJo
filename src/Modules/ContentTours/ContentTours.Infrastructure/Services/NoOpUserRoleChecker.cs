using ContentTours.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ContentTours.Infrastructure.Services;

internal sealed class NoOpUserRoleChecker(
    ILogger<NoOpUserRoleChecker> logger)
    : IUserRoleChecker
{
    public Task<bool> HasRoleAsync(
        Guid userId,
        string roleName,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "NoOpUserRoleChecker.HasRoleAsync called for UserId={UserId} RoleName={RoleName}; returning true (stub).",
            userId, roleName);
        return Task.FromResult(true);
    }
}
