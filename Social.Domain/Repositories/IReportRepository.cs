using Social.Domain.Entities;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Social.Domain.Repositories;

public interface IReportRepository : IRepository<Report, Guid>
{
    Task<IReadOnlyList<Report>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
    Task<int> CountByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
    Task<IReadOnlyList<Report>> GetPendingAsync(CancellationToken ct = default);
}
