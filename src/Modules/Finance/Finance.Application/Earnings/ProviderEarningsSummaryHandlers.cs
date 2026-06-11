using Finance.Contracts.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Earnings;

/// <summary>
/// Rich provider earnings summary exposed at GET /api/v1/finance/provider/summary ([Backend] B4).
/// Surfaces the payout-derived <see cref="GuideEarningsSummary"/> (this-month / pending-payout /
/// commission breakdown) that previously was only consumed internally by ContentTours guide endpoints.
/// Additive — the legacy /finance/guide/summary payment-based summary is untouched.
/// </summary>
public sealed record ProviderEarningsSummaryDto(
    decimal GrossTotal,
    decimal NetEarnings,
    decimal ThisMonth,
    decimal PendingPayout,
    decimal TotalCommission,
    string Currency);

public sealed record GetProviderEarningsSummaryQuery(Guid ProviderUserId) : IRequest<Result<ProviderEarningsSummaryDto>>;

public sealed class GetProviderEarningsSummaryQueryHandler(
    IGuideEarningReader earningReader,
    ILogger<GetProviderEarningsSummaryQueryHandler> logger)
    : IRequestHandler<GetProviderEarningsSummaryQuery, Result<ProviderEarningsSummaryDto>>
{
    public async Task<Result<ProviderEarningsSummaryDto>> Handle(GetProviderEarningsSummaryQuery request, CancellationToken ct)
    {
        var summary = await earningReader.GetSummaryAsync(request.ProviderUserId, ct);
        logger.LogDebug("Read provider earnings summary for {ProviderUserId}.", request.ProviderUserId);
        return Result.Success(new ProviderEarningsSummaryDto(
            summary.TotalEarned,
            summary.NetEarnings,
            summary.ThisMonth,
            summary.PendingPayout,
            summary.CommissionDeducted,
            summary.Currency));
    }
}
