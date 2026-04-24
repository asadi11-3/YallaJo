using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace Security.Application.Queries.GetAuditLogs;

/// <summary>
/// Phase 4 — query for the paginated admin audit timeline. All filters
/// are optional and back-compatible: legacy callers passing only
/// <c>Page</c>/<c>PageSize</c>/<c>UserId</c> behave exactly as before.
/// </summary>
/// <param name="UserId">Filter by SUBJECT user (the user the action targeted).</param>
/// <param name="ActorUserId">Phase 4 — filter by ADMIN actor.</param>
/// <param name="Action">Phase 4 — filter by exact action verb (case-sensitive, see <c>AuditActions</c>).</param>
/// <param name="From">Phase 4 — inclusive lower bound on <c>OccurredAt</c>.</param>
/// <param name="To">Phase 4 — inclusive upper bound on <c>OccurredAt</c>.</param>
public sealed record GetAuditLogsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? UserId = null,
    Guid? ActorUserId = null,
    string? Action = null,
    DateTime? From = null,
    DateTime? To = null) : IQuery<PaginatedResult<AuditLogDto>>;
