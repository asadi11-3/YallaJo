using MediatR;
using Messaging.Application.Caching;
using Messaging.Application.Queries.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetSupportTicketById;

public sealed record GetSupportTicketByIdQuery(Guid TicketId, Guid CallerUserId, bool IsAdmin) : IRequest<Result<SupportTicketDto>>, ICacheableQuery
{
    public string CacheKey => MessagingCacheKeys.SupportTicket(TicketId, CallerUserId, IsAdmin);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [MessagingCacheKeys.SupportTicketTag(TicketId)];
}
