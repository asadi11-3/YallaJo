using MediatR;
using Messaging.Application.Queries.Dtos;
using Messaging.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetSupportTicketById;

internal sealed class GetSupportTicketByIdQueryHandler(
    ISupportTicketRepository ticketRepository) : IRequestHandler<GetSupportTicketByIdQuery, Result<SupportTicketDto>>
{
    public async Task<Result<SupportTicketDto>> Handle(GetSupportTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdWithMessagesAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure<SupportTicketDto>(new Error("SupportTicket.NotFound", "Ticket not found."), Outcome.NotFound);

        if (!request.IsAdmin && ticket.CreatedByUserId != request.CallerUserId)
            return Result.Failure<SupportTicketDto>(new Error("SupportTicket.OwnerMismatch", "You do not own this ticket."), Outcome.Forbidden);

        var messages = ticket.TicketMessages?.Select(m => new TicketMessageDto(m.Id, m.AuthorUserId, m.Body, m.IsInternal, m.CreatedAt)).ToList()
                       ?? new List<TicketMessageDto>();

        var dto = new SupportTicketDto(
            ticket.Id, ticket.CreatedByUserId, ticket.Category.ToString(), ticket.Subject,
            ticket.Priority.ToString(), ticket.Status.ToString(), ticket.SlaBreachAt,
            ticket.AssignedToUserId, ticket.AssignedAt, ticket.ResolvedByUserId, ticket.ResolvedAt,
            ticket.ResolutionNotes, ticket.ClosedAt, ticket.CreatedAt,
            ticket.RowVersion is null or { Length: 0 } ? string.Empty : Convert.ToBase64String(ticket.RowVersion),
            messages);

        return Result.Success(dto);
    }
}
