using Accounts.Domain.Enums;

namespace Accounts.Application.Queries.GetAdminProviderQueue;

public sealed record GetAdminProviderQueueResult(
    IReadOnlyList<ProviderApplicationSummary> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ProviderApplicationSummary(
    Guid ApplicationId,
    Guid UserId,
    ProviderType Type,
    string BusinessName,
    string ContactEmail,
    ProviderApplicationStatus Status,
    DateTime? SubmittedAt,
    DateTime? ReviewedAt,
    int DocumentCount,
    int ReapplicationCount);
