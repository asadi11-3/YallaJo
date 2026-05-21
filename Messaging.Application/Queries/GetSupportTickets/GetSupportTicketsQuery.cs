using MediatR;
using Messaging.Application.Queries.Dtos;
using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetSupportTickets;

public sealed record GetSupportTicketsQuery(
    Guid? UserId,          // null = admin view all
    bool IsAdmin,
    TicketStatus? Status = null,
    TicketCategory? Category = null,
    Guid? AfterCursor = null,
    int PageSize = 20) : IRequest<Result<SupportTicketPageDto>>;
