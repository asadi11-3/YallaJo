using System.Globalization;
using System.Runtime.CompilerServices;
using Analytics.Domain.Entities;
using Analytics.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.ExportAuditLogs;

public sealed class ExportAuditLogsQueryHandler(IAuditLogRepository repo, ILogger<ExportAuditLogsQueryHandler> logger) : IQueryHandler<ExportAuditLogsQuery, IAsyncEnumerable<string>>
{
    private const string Header = "Id,UserId,Action,EntityType,EntityId,OccurredAt";

    public Task<Result<IAsyncEnumerable<string>>> Handle(ExportAuditLogsQuery request, CancellationToken ct)
    {
        logger.LogDebug("Streaming audit log export from {From} to {To}", request.From, request.To);
        return Task.FromResult(Result<IAsyncEnumerable<string>>.Success(StreamCsvLines(request.From, request.To, ct)));
    }

    private async IAsyncEnumerable<string> StreamCsvLines(DateTime from, DateTime to, [EnumeratorCancellation] CancellationToken ct)
    {
        yield return Header;

        await foreach (var log in repo.StreamAsync(from, to, ct).WithCancellation(ct))
        {
            yield return FormatLine(log);
        }
    }

    private static string FormatLine(AuditLog log) => string.Join(',',
        Escape(log.Id.ToString(CultureInfo.InvariantCulture)),
        Escape(log.UserId?.ToString() ?? string.Empty),
        Escape(log.Action.ToString()),
        Escape(log.EntityType),
        Escape(log.EntityId.ToString()),
        Escape(log.OccurredAt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)));

    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\r') && !value.Contains('\n'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
