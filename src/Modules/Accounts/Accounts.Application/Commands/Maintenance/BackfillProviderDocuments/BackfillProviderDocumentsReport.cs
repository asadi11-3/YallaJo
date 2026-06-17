namespace Accounts.Application.Commands.Maintenance.BackfillProviderDocuments;

/// <summary>
/// Structured backfill report returned by <see cref="BackfillProviderDocumentsCommand"/>.
/// </summary>
/// <param name="RunId">A GUIDv7 correlation id for this run. Surfaced in logs.</param>
/// <param name="DryRun">Echoes the dry-run mode chosen at the endpoint.</param>
/// <param name="Total">Total number of source rows inspected this invocation.</param>
/// <param name="Inserted">
/// Rows for which a new FileAsset + ProviderDocumentFile link was created. In dry-run
/// mode this counts rows that WOULD HAVE been inserted.
/// </param>
/// <param name="Reused">
/// Rows for which an existing FileAsset (by StorageKey) was reused. A new
/// ProviderDocumentFile link was still created (unless dry-run / already linked).
/// </param>
/// <param name="Errors">Number of rows that failed with an unexpected exception.</param>
/// <param name="SkippedByReason">
/// Histogram of soft skips keyed by reason: <c>NoFileUrl</c>, <c>InvalidPrefix</c>,
/// <c>InvalidPath</c>, <c>InvalidExtension</c>, <c>FileNotFound</c>, <c>AlreadyLinked</c>.
/// </param>
/// <param name="NextAfterId">
/// Resume cursor. Pass back to the next invocation as <c>AfterId</c> to continue.
/// </param>
/// <param name="HasMore">
/// True when the last batch returned a full page — the caller should re-invoke
/// with <see cref="NextAfterId"/>.
/// </param>
public sealed record BackfillProviderDocumentsReport(
    Guid RunId,
    bool DryRun,
    int Total,
    int Inserted,
    int Reused,
    int Errors,
    IReadOnlyDictionary<string, int> SkippedByReason,
    Guid? NextAfterId,
    bool HasMore);
