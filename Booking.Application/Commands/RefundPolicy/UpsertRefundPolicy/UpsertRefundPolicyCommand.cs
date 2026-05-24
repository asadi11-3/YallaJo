using Booking.Application.Queries.GetRefundPolicyByTour;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.RefundPolicy.UpsertRefundPolicy;

public sealed record UpsertRefundPolicyCommand(
    Guid TourId,
    IReadOnlyList<RefundTierDto> Tiers) : ICommand<RefundPolicyDto>;
