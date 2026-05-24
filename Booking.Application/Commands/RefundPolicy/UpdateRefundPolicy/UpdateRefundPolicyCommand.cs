using Booking.Application.Queries.GetRefundPolicyByTour;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.RefundPolicy.UpdateRefundPolicy;

public sealed record UpdateRefundPolicyCommand(
    Guid PolicyId,
    IReadOnlyList<RefundTierDto> Tiers,
    byte[] RowVersion) : ICommand<RefundPolicyDto>;
