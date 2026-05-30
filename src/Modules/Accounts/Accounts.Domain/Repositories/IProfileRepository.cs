using System.Linq.Expressions;
using Accounts.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Accounts.Domain.Repositories;

public interface IProfileRepository : IRepository<Profile, Guid>
{
    Task<Profile?> FirstOrDefaultIncludingDeletedAsync(
        Expression<Func<Profile, bool>> filter,
        bool asNoTracking = false,
        CancellationToken ct = default);
}
