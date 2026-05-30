using MediatR;
using Messaging.Application.Queries.Dtos;
using Messaging.Domain.Entities;
using Messaging.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetSupportTickets;

internal sealed class GetSupportTicketsQueryHandler(
    ISupportTicketRepository ticketRepository) : IRequestHandler<GetSupportTicketsQuery, Result<SupportTicketPageDto>>
{
    public async Task<Result<SupportTicketPageDto>> Handle(GetSupportTicketsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        (IReadOnlyList<SupportTicket> Items, Guid? NextCursor) result;

        if (request.IsAdmin)
            result = await ticketRepository.GetByAdminPagedAsync(request.Status, request.Category, request.AfterCursor, pageSize, cancellationToken);
        else
            result = await ticketRepository.GetByUserPagedAsync(request.UserId!.Value, request.AfterCursor, pageSize, cancellationToken);

        var dtos = result.Items.Select(t => new SupportTicketDto(
            t.Id, t.CreatedByUserId, t.Category.ToString(), t.Subject, t.Priority.ToString(), t.Status.ToString(),
            t.SlaBreachAt, t.AssignedToUserId, t.AssignedAt, t.ResolvedByUserId, t.ResolvedAt,
            t.ResolutionNotes, t.ClosedAt, t.CreatedAt)).ToList();

        return Result.Success(new SupportTicketPageDto(dtos, result.NextCursor));
    }
}
