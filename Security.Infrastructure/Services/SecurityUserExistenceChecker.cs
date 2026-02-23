using Security.Contracts.Abstractions;
using Security.Domain.Repositories;

namespace Security.Infrastructure.Services;

/// <summary>
/// Checks user existence via the Security module's own repository.
/// The global query filter on User (IsDeleted) is applied automatically.
/// </summary>
internal sealed class SecurityUserExistenceChecker(IUserRepository userRepository)
    : ISecurityUserExistenceChecker
{
    public async Task<bool> ExistsAsync(Guid userId, CancellationToken ct = default)
        => await userRepository.ExistsAsync(u => u.Id == userId, ct);
}