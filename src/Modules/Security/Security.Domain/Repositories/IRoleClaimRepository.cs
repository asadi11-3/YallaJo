using Security.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Security.Domain.Repositories;

public interface IRoleClaimRepository : IReadRepository<RoleClaim, Guid>, IWriteRepository<RoleClaim, Guid>
{
}
