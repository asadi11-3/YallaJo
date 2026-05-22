namespace Accounts.Contracts.Abstractions;

/// <summary>
/// Cross-module contract for looking up user email addresses.
/// Used by Analytics email digest to find users with marketing consent.
/// </summary>
public interface IUserEmailLookupService
{
    /// <summary>
    /// Returns emails of users who have opted in to email digest notifications.
    /// </summary>
    Task<IReadOnlyList<UserEmailInfo>> GetDigestSubscribersAsync(int limit, CancellationToken ct = default);
}

public sealed record UserEmailInfo(Guid UserId, string Email, string? DisplayName);
