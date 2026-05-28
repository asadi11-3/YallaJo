using Finance.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Earnings;

public sealed record GuideEarningDto(Guid PaymentId, Guid BookingId, decimal GrossAmount, decimal NetEstimate, string Currency, DateTime? PaidAt);

public sealed record GuideEarningsSummaryDto(decimal GrossTotal, decimal NetEstimateTotal, int PaymentCount, string Currency);

public sealed record AdminFinanceDashboardDto(decimal GrossRevenue, decimal PlatformCommissionEstimate, decimal ProviderNetEstimate, int CompletedPaymentCount, int OpenDisputeCount, string Currency);

public sealed record GetGuideEarningsQuery(Guid GuideId, DateTime? FromUtc, DateTime? ToUtc) : IRequest<Result<IReadOnlyList<GuideEarningDto>>>;

public sealed record GetGuideEarningsSummaryQuery(Guid GuideId, DateTime? FromUtc, DateTime? ToUtc) : IRequest<Result<GuideEarningsSummaryDto>>;

public sealed record GetAdminFinanceDashboardQuery(DateTime? FromUtc, DateTime? ToUtc) : IRequest<Result<AdminFinanceDashboardDto>>;

public sealed class GetGuideEarningsQueryHandler(IPaymentRepository paymentRepository, ILogger<GetGuideEarningsQueryHandler> logger)
    : IRequestHandler<GetGuideEarningsQuery, Result<IReadOnlyList<GuideEarningDto>>>
{
    public async Task<Result<IReadOnlyList<GuideEarningDto>>> Handle(GetGuideEarningsQuery request, CancellationToken ct)
    {
        var payments = await paymentRepository.GetCompletedProviderPaymentsAsync(request.GuideId, request.FromUtc, request.ToUtc, ct);
        logger.LogDebug("Read {Count} guide earnings for {GuideId}.", payments.Count, request.GuideId);
        return Result.Success<IReadOnlyList<GuideEarningDto>>(payments
            .Where(p => p.BookingId.HasValue)
            .Select(p => new GuideEarningDto(p.Id, p.BookingId!.Value, p.Amount.Amount, Math.Round(p.Amount.Amount * 0.85m, 2), p.Currency, p.PaidAt))
            .ToList());
    }
}

public sealed class GetGuideEarningsSummaryQueryHandler(IPaymentRepository paymentRepository, ILogger<GetGuideEarningsSummaryQueryHandler> logger)
    : IRequestHandler<GetGuideEarningsSummaryQuery, Result<GuideEarningsSummaryDto>>
{
    public async Task<Result<GuideEarningsSummaryDto>> Handle(GetGuideEarningsSummaryQuery request, CancellationToken ct)
    {
        var payments = await paymentRepository.GetCompletedProviderPaymentsAsync(request.GuideId, request.FromUtc, request.ToUtc, ct);
        var currency = payments.FirstOrDefault()?.Currency ?? "JOD";
        var gross = payments.Sum(p => p.Amount.Amount);
        logger.LogDebug("Read guide earnings summary for {GuideId}.", request.GuideId);
        return Result.Success(new GuideEarningsSummaryDto(gross, Math.Round(gross * 0.85m, 2), payments.Count, currency));
    }
}

public sealed class GetAdminFinanceDashboardQueryHandler(
    IPaymentRepository paymentRepository,
    IDisputeRepository disputeRepository,
    ILogger<GetAdminFinanceDashboardQueryHandler> logger)
    : IRequestHandler<GetAdminFinanceDashboardQuery, Result<AdminFinanceDashboardDto>>
{
    public async Task<Result<AdminFinanceDashboardDto>> Handle(GetAdminFinanceDashboardQuery request, CancellationToken ct)
    {
        var payments = await paymentRepository.GetCompletedPaymentsAsync(request.FromUtc, request.ToUtc, ct);
        var disputes = await disputeRepository.GetOpenDisputesAsync(ct);
        var gross = payments.Sum(p => p.Amount.Amount);
        var commission = Math.Round(gross * 0.15m, 2);
        var currency = payments.FirstOrDefault()?.Currency ?? "JOD";
        logger.LogDebug("Read admin finance dashboard.");
        return Result.Success(new AdminFinanceDashboardDto(gross, commission, gross - commission, payments.Count, disputes.Count, currency));
    }
}
