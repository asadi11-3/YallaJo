using Security.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Security.Domain.Repositories;

public interface IRoleRepository : IReadRepository<Role,Guid>, IWriteRepository<Role, Guid>
{
    Task<Dictionary<Guid, IReadOnlyList<string>>> GetRolesByUserIdsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default);

    Task<IReadOnlyList<Role>> GetRolesByNamesAsync(
        IEnumerable<string> roleNames, CancellationToken ct = default);

    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);

    Task<IReadOnlyList<Role>> GetAllActiveAsync(CancellationToken ct = default);
}