using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace YallaJo.SharedKernel.Application.Abstractions.Outbox;

/// <summary>
/// Returns dead-lettered outbox messages across all modules.
/// </summary>
/// <param name="Module">Optional: filter to a specific module. Null = all modules.</param>
/// <param name="Limit">Maximum rows per module (default 50).</param>
public sealed record ListDeadLettersQuery(string? Module = null, int Limit = 50)
    : IQuery<ListDeadLettersResult>;

/// <summary>Aggregated dead-letter list across all (or a specific) module.</summary>
public sealed record ListDeadLettersResult(
    IReadOnlyList<DeadLetterEntry> Items,
    int TotalCount);

/// <summary>Single dead-lettered message entry.</summary>
public sealed record DeadLetterEntry(
    Guid Id,
    string Module,
    string Type,
    DateTime OccurredOnUtc,
    int RetryCount,
    string? LastError);
