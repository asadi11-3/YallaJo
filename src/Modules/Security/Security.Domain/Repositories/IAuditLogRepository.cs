
using Security.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Security.Domain.Repositories;

public interface IAuditLogRepository : IReadRepository<AuditLog, Guid>, IWriteRepository<AuditLog, Guid>;
