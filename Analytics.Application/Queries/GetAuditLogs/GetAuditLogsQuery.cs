using Analytics.Application.Models;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetAuditLogs;

public sealed record GetAuditLogsQuery(string? EntityType, Guid? EntityId, Guid? UserId, string? Action, DateTime? From, DateTime? To, long? AfterId, int PageSize) : IQuery<CursorPageDto<AdminAuditLogDto>>;
