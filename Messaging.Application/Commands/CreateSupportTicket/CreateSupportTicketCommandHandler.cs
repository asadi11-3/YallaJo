using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.CreateSupportTicket;

internal sealed class CreateSupportTicketCommandHandler(
    ISupportTicketRepository ticketRepository,
    IMessagingUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<CreateSupportTicketCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = SupportTicket.Create(
            request.CreatedByUserId, request.Category, request.Subject, request.Body, timeProvider);

        await ticketRepository.AddAsync(ticket, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ticket.Id);
    }
}
