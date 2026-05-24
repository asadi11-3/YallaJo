using Booking.Application.Queries.JoinRequest;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.JoinRequest.RejectJoinRequest;

public sealed record RejectJoinRequestCommand(
    Guid JoinRequestId,
    string? Reason) : ICommand<JoinRequestDto>;
