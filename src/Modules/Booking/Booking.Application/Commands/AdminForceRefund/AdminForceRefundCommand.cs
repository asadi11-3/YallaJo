using Booking.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.AdminForceRefund;

/// <summary>
/// Admin-only force-majeure cancellation: bypasses any RefundPolicy and refunds 100% of TotalAmount.
/// Maps to POST /api/v1/admin/bookings/{id}/force-refund.
/// </summary>
public sealed record AdminForceRefundCommand(Guid BookingId, string Reason) : ICommand<AdminForceRefundResult>;

public sealed record AdminForceRefundResult(
    Guid BookingId,
    BookingStatus Status,
    DateTime CancelledAt,
    CancellationSource Source,
    string Reason,
    decimal RefundAmount,
    string Currency);
