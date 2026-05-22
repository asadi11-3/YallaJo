using MediatR;
using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.CreateSupportTicket;

public sealed record CreateSupportTicketCommand(
    Guid CreatedByUserId,
    TicketCategory Category,
    string Subject,
    string Body) : IRequest<Result<Guid>>;
