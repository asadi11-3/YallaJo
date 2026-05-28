using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.ApproveJoinRequest;

/// <summary>Guide/provider approves a join request, creating a new booking for the joiner.</summary>
public sealed record ApproveJoinRequestCommand(
    Guid JoinRequestId,
    string? ResponseMessage)
    : ICommand;
