using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.AssignSupportTicket;

public sealed record AssignSupportTicketCommand(Guid TicketId, Guid AdminUserId, Guid AssignedByUserId) : IRequest<Result>;
