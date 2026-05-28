using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.SubmitJoinRequest;

/// <summary>User requests to join an existing confirmed group booking.</summary>
public sealed record SubmitJoinRequestCommand(
    Guid TourBookingId,
    Guid AvailabilitySlotId,
    int ParticipantCount,
    string? Message)
    : ICommand<Guid>;
