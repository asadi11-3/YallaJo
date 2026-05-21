using Social.Domain.Entities;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Social.Domain.Repositories;

/// <summary>Repository for <see cref="Report"/> aggregate.</summary>
public interface IReportRepository : IRepository<Report, Guid>
{
    /// <summary>Returns open/under-review reports for a given entity.</summary>
    Task<IReadOnlyList<Report>> GetByEntityAsync(
        ReportableEntityType entityType, Guid entityId, CancellationToken ct = default);

    /// <summary>Counts unique reporters for a given entity (drives S-R5 auto-hide logic).</summary>
    Task<int> CountUniqueReportersAsync(
        ReportableEntityType entityType, Guid entityId, CancellationToken ct = default);

    /// <summary>Returns reports pending admin review (cursor-paginated).</summary>
    Task<(IReadOnlyList<Report> Items, Guid? NextCursor)> GetAdminPageAsync(
        Guid? afterId, int pageSize, CancellationToken ct = default);

    /// <summary>Returns whether the reporter already submitted an open report for this entity.</summary>
    Task<bool> ExistsOpenByReporterAsync(
        Guid reporterUserId, ReportableEntityType entityType, Guid entityId, CancellationToken ct = default);
}
