namespace Analytics.Application.Interfaces;

public interface IAuditLogRedactor
{
    (string? RedactedValue, IReadOnlyList<string> RedactedFields) Redact(string? json);
}
