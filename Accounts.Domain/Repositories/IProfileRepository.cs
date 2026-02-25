using Accounts.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Accounts.Domain.Repositories;

public interface IProfileRepository : IRepository<Profile, Guid>
{
    Task<Profile?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken ct);
}
