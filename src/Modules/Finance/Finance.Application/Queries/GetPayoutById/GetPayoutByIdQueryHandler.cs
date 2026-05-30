using Finance.Application.Queries.Dtos;
using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetPayoutById;

public sealed class GetPayoutByIdQueryHandler(IPayoutRepository payoutRepository)
    : IRequestHandler<GetPayoutByIdQuery, Result<PayoutDto>>
{
    public async Task<Result<PayoutDto>> Handle(GetPayoutByIdQuery request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var payout = await payoutRepository.GetByIdAsync(request.PayoutId, ct);
        if (payout is null)
        {
            return Result.Failure<PayoutDto>(new Error("Payout.NotFound", $"Payout {request.PayoutId} not found."), Outcome.NotFound);
        }

        var isProviderSelf = request.CallerProviderId is { } pid && pid == payout.ProviderId;
        if (!request.CallerIsAdmin && !isProviderSelf)
        {
            return Result.Failure<PayoutDto>(new Error("Payout.OwnerMismatch", "Caller is not authorised to view this payout."), Outcome.Forbidden);
        }

        return Result.Success(MapToDto(payout));
    }

    internal static PayoutDto MapToDto(Payout p) => new(
        p.Id,
        p.ProviderId,
        p.Currency,
        p.BatchPeriodStart,
        p.BatchPeriodEnd,
        p.GrossAmount.Amount,
        p.CommissionAmount.Amount,
        p.NetAmount.Amount,
        p.Status.ToString(),
        p.ApprovedByUserId,
        p.ApprovedAt,
        p.GatewayPayoutId,
        p.CompletedAt,
        p.FailureReason,
        p.BankAccountId,
        p.PayoutItems.Count,
        p.PayoutItems.Select(MapItemToDto).ToList(),
        p.CreatedAt,
        p.UpdatedAt);

    internal static PayoutItemDto MapItemToDto(PayoutItem i) => new(
        i.Id,
        i.BookingId,
        i.GrossAmount.Amount,
        i.CommissionAmount.Amount,
        i.NetAmount.Amount,
        i.GrossAmount.Currency,
        i.CommissionRuleSnapshotId);
}
