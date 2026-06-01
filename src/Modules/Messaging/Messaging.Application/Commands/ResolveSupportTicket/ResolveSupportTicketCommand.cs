using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.ResolveSupportTicket;

public sealed record ResolveSupportTicketCommand(Guid TicketId, Guid ResolvedByUserId, string? ResolutionNotes, byte[] RowVersion) : IRequest<Result>;
