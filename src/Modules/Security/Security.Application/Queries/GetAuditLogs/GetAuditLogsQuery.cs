using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace Security.Application.Queries.GetAuditLogs;

public sealed record GetAuditLogsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? UserId = null,
    Guid? ActorUserId = null,
    string? Action = null,
    DateTime? From = null,
    DateTime? To = null) : IQuery<PaginatedResult<AuditLogDto>>;
