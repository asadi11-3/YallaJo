namespace YallaJo.SharedKernel.Infrastructure.Outbox;

/// <summary>
/// Read model for a dead-lettered outbox message returned by ops endpoints.
/// </summary>
public sealed record OutboxDeadLetterDto(
    Guid Id,
    string Module,
    string Type,
    DateTime OccurredOnUtc,
    int RetryCount,
    string? LastError);
