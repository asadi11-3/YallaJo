using Security.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Security.Domain.Repositories;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByIdWithEmailsAsync(Guid userId, CancellationToken ct = default);
}
