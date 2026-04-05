using Accounts.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Accounts.Domain.Repositories;

// GetByUserIdAsync removed — use profileRepository.FirstOrDefaultAsync(p => p.UserId == userId, ...)
// from the inherited IReadRepository<Profile, Guid> surface instead.
public interface IProfileRepository : IRepository<Profile, Guid>
{
}
