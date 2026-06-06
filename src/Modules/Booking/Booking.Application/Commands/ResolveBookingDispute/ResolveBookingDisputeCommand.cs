using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.ResolveBookingDispute;

/// <summary>
/// Admin resolves a Disputed booking. Terminal state for the dispute lifecycle.
/// </summary>
public sealed record ResolveBookingDisputeCommand(Guid BookingId, string ResolutionNotes)
    : ICommand<ResolveBookingDisputeResult>;
