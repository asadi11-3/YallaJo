namespace Accounts.Application.Abstractions;

public interface ISecurityUserExistenceChecker
{
    Task<bool> ExistsAsync(Guid userId, CancellationToken ct = default);
}