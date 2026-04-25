using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Security.Infrastructure.Repositories;

internal sealed class AuditLogRepository(SecurityDbContext context)
    : EfEntityRepository<AuditLog, Guid>(context), IAuditLogRepository;
