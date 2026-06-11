using MediatR;
using Messaging.Application.Caching;
using Messaging.Application.Queries.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetSupportTicketStatusCounts;

/// <summary>
/// Admin-only: support ticket queue counts grouped by status (counted tabs).
/// Cached under the admin tickets tag so assign/resolve/close invalidate it.
/// </summary>
public sealed record GetSupportTicketStatusCountsQuery : IRequest<Result<SupportTicketStatusCountsDto>>, ICacheableQuery
{
    public string CacheKey => MessagingCacheKeys.SupportTicketStatusCounts;
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [MessagingCacheKeys.SupportTicketsAdminTag];
}
