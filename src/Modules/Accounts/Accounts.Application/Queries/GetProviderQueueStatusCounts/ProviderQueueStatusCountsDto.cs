namespace Accounts.Application.Queries.GetProviderQueueStatusCounts;

/// <summary>
/// Per-status counts for the admin provider application queue.
/// <c>AwaitingDocuments</c> maps the <c>MoreDocsNeeded</c> status; drafts are
/// excluded because they never appear in the admin queue.
/// </summary>
public sealed record ProviderQueueStatusCountsDto(
    int Pending,
    int AwaitingDocuments,
    int Approved,
    int Suspended,
    int Rejected);
