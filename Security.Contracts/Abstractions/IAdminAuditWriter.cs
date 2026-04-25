namespace Security.Contracts.Abstractions;

public interface IAdminAuditWriter
{
    Task RecordAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default);
}
