using Booking.Application.Queries.JoinRequest;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.JoinRequest.CreateJoinRequest;

public sealed record CreateJoinRequestCommand(
    Guid BookingId,
    int ParticipantCount,
    string? Message) : ICommand<JoinRequestDto>;
