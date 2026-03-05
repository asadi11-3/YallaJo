using Security.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Security.Domain.Repositories;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByIdWithEmailsAsync(Guid userId, CancellationToken ct = default);
    Task<User?> GetByEmailWithDetailsAsync(string normalizedEmail, CancellationToken ct = default);
    Task<User?> GetByIdWithDetailsAsync(Guid userId, CancellationToken ct = default);
    Task<User?> GetByIdWithPhonesAsync(Guid userId, CancellationToken ct = default);
    Task<bool> AnyWithRoleAsync(string roleName, CancellationToken ct = default);

    Task<UserRole?> GetUserRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default);

    Task<(List<User> Items, int TotalCount)> GetPagedWithDetailsAsync(
        int page, int pageSize, CancellationToken ct = default);

    void RemoveUserRole(UserRole userRole);
}
