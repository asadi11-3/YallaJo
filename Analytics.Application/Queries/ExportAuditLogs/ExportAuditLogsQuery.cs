using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.ExportAuditLogs;

public sealed record ExportAuditLogsQuery(DateTime From, DateTime To) : IQuery<string>;
