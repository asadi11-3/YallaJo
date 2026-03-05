using Security.Contracts.Abstractions;
using Security.Domain.Repositories;

namespace Security.Infrastructure.Services;


internal sealed class SecurityUserExistenceChecker(IUserRepository userRepository)
    : ISecurityUserExistenceChecker
{
    public async Task<bool> ExistsAsync(Guid userId, CancellationToken ct = default)
        => await userRepository.ExistsAsync(u => u.Id == userId, ct);
}