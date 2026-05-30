using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Commands.TriggerPayout;

/// <summary>
/// Sweeps escrow-eligible payments into Payout aggregates grouped by (ProviderId, Currency).
/// Triggered manually by admin (POST /payouts/admin/trigger) or by T5 PayoutBatchingService.
/// </summary>
public sealed record TriggerPayoutCommand(bool IsManual) : IRequest<Result<TriggerPayoutResult>>;

public sealed record TriggerPayoutResult(
    int CreatedCount,
    int SkippedCount,
    int OnHoldCount,
    decimal TotalSweptAmount);
