using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Auth.Infrastructure.Repositories;

public sealed class SessionRepository(AuthDbContext context)
    : EfRepository<Session, Guid>(context), ISessionRepository
{
   
}
