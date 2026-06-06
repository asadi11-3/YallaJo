using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.OpenBookingDispute;

/// <summary>
/// User opens a dispute on a Completed booking within the 48-hour post-completion window.
/// </summary>
public sealed record OpenBookingDisputeCommand(Guid BookingId, string Reason)
    : ICommand<OpenBookingDisputeResult>;
