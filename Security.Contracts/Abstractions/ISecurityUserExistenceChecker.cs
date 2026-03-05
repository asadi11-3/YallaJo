namespace Security.Contracts.Abstractions;


public interface ISecurityUserExistenceChecker
{
    Task<bool> ExistsAsync(Guid userId, CancellationToken ct = default);
}