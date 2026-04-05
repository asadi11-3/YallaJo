using Auth.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Auth.Domain.Repositories;

public interface ISessionRepository : IRepository<Session, Guid>
{
}
