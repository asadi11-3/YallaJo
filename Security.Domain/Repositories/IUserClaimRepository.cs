using Security.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Security.Domain.Repositories;

public interface IUserClaimRepository : IReadRepository<UserClaim, Guid>, IWriteRepository<UserClaim, Guid>
{
}
