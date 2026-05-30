using MediatR;
using Messaging.Application.Caching;
using Messaging.Application.Queries.Dtos;
using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetSupportTickets;

public sealed record GetSupportTicketsQuery(
    Guid? UserId,          // null = admin view all
    bool IsAdmin,
    TicketStatus? Status = null,
    TicketCategory? Category = null,
    Guid? AfterCursor = null,
    int PageSize = 20) : IRequest<Result<SupportTicketPageDto>>, ICacheableQuery
{
    public string CacheKey => MessagingCacheKeys.SupportTickets(UserId, IsAdmin, Status?.ToString(), Category?.ToString(), AfterCursor, Math.Clamp(PageSize, 1, 50));
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => IsAdmin || UserId is null
        ? [MessagingCacheKeys.SupportTicketsAdminTag]
        : [MessagingCacheKeys.SupportTicketsTag(UserId.Value)];
}
