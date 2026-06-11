using Security.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace Security.Domain.Repositories;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByIdWithEmailsAsync(Guid userId, CancellationToken ct = default);
    Task<User?> GetByEmailWithDetailsAsync(string normalizedEmail, CancellationToken ct = default);
    Task<User?> GetByIdWithDetailsAsync(Guid userId, CancellationToken ct = default);
    Task<User?> GetByIdWithPhonesAsync(Guid userId, CancellationToken ct = default);
    Task<bool> AnyWithRoleAsync(string roleName, CancellationToken ct = default);
    Task<int> CountUsersInRolesExcludingAssignmentAsync(
        IReadOnlyCollection<string> roleNames,
        Guid excludedUserId,
        Guid excludedRoleId,
        CancellationToken ct = default);
    Task<UserRole?> GetUserRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default);
    Task<PaginatedResult<User>> GetPagedWithDetailsAsync(int page, int pageSize, CancellationToken ct = default);
    void RemoveUserRole(UserRole userRole);
    Task<Guid?> GetUserIdByEmailAsync(string normalizedEmail, CancellationToken ct = default);
    Task<string?> GetPrimaryPhoneNumberAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Stored password hash for the user, or null when the user does not exist. B6.</summary>
    Task<string?> GetPasswordHashAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Users whose primary email contains the query fragment, with primary emails loaded.
    /// Single query, ordered by email, capped at <paramref name="limit"/> — feeds admin typeahead lookups.
    /// </summary>
    Task<IReadOnlyList<User>> SuggestByEmailAsync(string query, int limit, CancellationToken ct = default);

    /// <summary>
    /// Lightweight lookup for typeahead pickers (B1): matches active users by primary-email
    /// substring (<paramref name="query"/>) and/or exact ids. Results capped at <paramref name="limit"/>.
    /// </summary>
    Task<IReadOnlyList<User>> SearchAsync(
        string? query,
        IReadOnlyCollection<Guid>? ids,
        int limit,
        CancellationToken ct = default);
}
