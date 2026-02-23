namespace Security.Contracts.Abstractions;

/// <summary>
/// Cross-module contract: allows other modules to check whether a Security user exists.
/// Implemented by Security.Infrastructure; consumed by any module that references Security.Contracts.
/// </summary>
public interface ISecurityUserExistenceChecker
{
    Task<bool> ExistsAsync(Guid userId, CancellationToken ct = default);
}