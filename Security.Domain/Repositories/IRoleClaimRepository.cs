using Security.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Security.Domain.Repositories
{
    public interface IRoleClaimRepository : IReadRepository<RoleClaim, Guid>, IWriteRepository<RoleClaim, Guid>
    {
        Task<IReadOnlyList<RoleClaim>> GetClaimsByRoleIdAsync(Guid roleId, CancellationToken ct = default);

        Task<IReadOnlyList<RoleClaim>> GetClaimsByRoleIdsAsync(IEnumerable<Guid> roleIds, CancellationToken ct = default);

        Task<IReadOnlyList<string>> GetClaimValuesByTypeAsync(Guid roleId, string claimType, CancellationToken ct = default);

        Task<bool> ExistsAsync(Guid roleId, string claimType, string claimValue, CancellationToken ct = default);

    }
}
