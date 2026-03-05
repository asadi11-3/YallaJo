using Security.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Security.Domain.Repositories;

public interface IUserClaimRepository : IReadRepository<UserClaim, Guid>, IWriteRepository<UserClaim, Guid>
{
    Task<bool> ExistsAsync(Guid userId, string claimType, string claimValue, CancellationToken ct = default);
}
