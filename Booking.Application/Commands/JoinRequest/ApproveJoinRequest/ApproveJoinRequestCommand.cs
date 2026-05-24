using Booking.Application.Queries.JoinRequest;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.JoinRequest.ApproveJoinRequest;

public sealed record ApproveJoinRequestCommand(Guid JoinRequestId) : ICommand<JoinRequestDto>;
