namespace Accounts.Contracts.Abstractions;

/// <summary>
/// Cross-module contract allowing other modules to check whether
/// a user holds an approved provider application.
/// </summary>
public interface IProviderStatusService
{
    /// <summary>
    /// Returns <c>true</c> when the user identified by <paramref name="userId"/>
    /// owns a <see cref="ProviderApplicationStatus.Approved"/> provider application.
    /// </summary>
    Task<bool> IsApprovedProviderAsync(Guid userId, CancellationToken cancellationToken = default);
}
