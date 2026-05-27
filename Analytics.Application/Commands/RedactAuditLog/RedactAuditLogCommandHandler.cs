using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Contracts.IntegrationEvents;
using Analytics.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.RedactAuditLog;

public sealed class RedactAuditLogCommandHandler(IAuditLogRepository repo, IAuditLogRedactor redactor, IAnalyticsUnitOfWork uow, IAnalyticsOutboxWriter outbox, ILogger<RedactAuditLogCommandHandler> logger) : ICommandHandler<RedactAuditLogCommand>
{
    public async Task<Result> Handle(RedactAuditLogCommand request, CancellationToken ct)
    {
        var entry = await repo.GetByIdAsync(request.Id, ct);
        if (entry is null) return Result.Failure(new Error("AuditLog.NotFound", "Audit log entry was not found."), Outcome.NotFound);
        var old = redactor.Redact(entry.OldValue); var @new = redactor.Redact(entry.NewValue);
        var fields = old.RedactedFields.Concat(@new.RedactedFields).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        entry.RetroactivelyRedact(request.AdminUserId, request.Reason, fields, old.RedactedValue, @new.RedactedValue, DateTime.UtcNow);
        await outbox.WriteAsync(new AuditLogEntryRedactedIntegrationEvent(entry.Id, entry.EntityType, entry.EntityId, request.AdminUserId.ToString(), DateTime.UtcNow), ct);
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Redacted audit log {Id}", request.Id);
        return Result.Success();
    }
}
