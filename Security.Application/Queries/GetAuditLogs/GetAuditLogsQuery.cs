// Security.Application/Queries/GetAuditLogs/GetAuditLogsQuery.cs
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace Security.Application.Queries.GetAuditLogs;

public sealed record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string Action,
    string? IpAddress,
    DateTime OccurredAt);

public sealed record GetAuditLogsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? UserId = null) : IQuery<PaginatedResult<AuditLogDto>>;
