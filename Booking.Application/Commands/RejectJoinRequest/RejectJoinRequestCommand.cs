using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.RejectJoinRequest;

/// <summary>Guide/provider rejects a join request.</summary>
public sealed record RejectJoinRequestCommand(
    Guid JoinRequestId,
    string? ResponseMessage)
    : ICommand;
