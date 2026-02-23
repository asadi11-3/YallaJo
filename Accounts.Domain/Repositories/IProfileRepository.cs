using Accounts.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Accounts.Domain.Interfaces;

public interface IProfileRepository : IRepository<Profile, Guid>
{
}
