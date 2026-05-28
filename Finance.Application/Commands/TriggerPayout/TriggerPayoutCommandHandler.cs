using Accounts.Contracts.Abstractions;
using Finance.Application.Interfaces;
using Finance.Contracts.Services;
using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Application.Commands.TriggerPayout;

/// <summary>
/// Builds Payout batches from currently escrow-eligible Payments.
/// Per F-R7: grouped by (ProviderId, Currency), commission applied per booking,
/// >LargePayoutThreshold remains Pending (needs admin Approve), else auto-MarksReadyForPayout.
/// </summary>
public sealed class TriggerPayoutCommandHandler(
    IPaymentRepository paymentRepository,
    IPayoutRepository payoutRepository,
    IProviderPaymentMethodRepository paymentMethodRepository,
    IAgencyAffiliationReadService agencyAffiliationReader,
    ICommissionLookupService commissionLookup,
    IFinanceUnitOfWork unitOfWork,
    IOptions<TriggerPayoutOptions> options,
    TimeProvider timeProvider,
    ILogger<TriggerPayoutCommandHandler> logger)
    : IRequestHandler<TriggerPayoutCommand, Result<TriggerPayoutResult>>
{
    private readonly TriggerPayoutOptions _options = options.Value;

    public async Task<Result<TriggerPayoutResult>> Handle(TriggerPayoutCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var asOf = now;
        var periodStart = DateOnly.FromDateTime(now.AddDays(-7));
        var periodEnd = DateOnly.FromDateTime(now);

        var eligible = await paymentRepository.GetEscrowReleaseEligibleAsync(asOf, ct);
        if (eligible.Count == 0)
        {
            return Result.Success(new TriggerPayoutResult(0, 0, 0, 0m));
        }

        var payouts = new Dictionary<(Guid ProviderId, string Currency), Payout>();
        var created = 0;
        var skipped = 0;
        var onHold = 0;
        decimal sweptTotal = 0m;

        foreach (var p in eligible)
        {
            if (!p.BookingId.HasValue)
            {
                skipped++;
                continue;
            }

            decimal commissionRate;
            try
            {
                var lookupResult = await commissionLookup.GetCommissionAsync(p.BookingId.Value, ct);
                commissionRate = lookupResult.Rate;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Commission lookup failed for payment {PaymentId}; defaulting to 0.15.", p.Id);
                commissionRate = 0.15m;
            }

            var currency = p.Currency;
            var grossAmount = p.Amount.Amount;
            var platformCommission = Math.Round(grossAmount * commissionRate, 2, MidpointRounding.ToEven);
            var postPlatformNet = grossAmount - platformCommission;
            var affiliation = await agencyAffiliationReader.GetActiveByGuideUserIdAsync(p.ProviderId, ct);

            if (affiliation is null)
            {
                var payout = await GetOrCreatePayoutAsync(p.ProviderId, currency, periodStart, periodEnd, payouts, ct);
                AddPayoutItem(payout, p.BookingId.Value, new Money(grossAmount, currency), new Money(platformCommission, currency), p.Id, ref skipped);
                continue;
            }

            var agencyAmount = Math.Round(postPlatformNet * (affiliation.CommissionPercentage / 100m), 2, MidpointRounding.ToEven);
            var guideAmount = postPlatformNet - agencyAmount;
            var guidePayout = await GetOrCreatePayoutAsync(p.ProviderId, currency, periodStart, periodEnd, payouts, ct);
            AddPayoutItem(guidePayout, p.BookingId.Value, new Money(grossAmount, currency), new Money(platformCommission + agencyAmount, currency), p.Id, ref skipped);

            var agencyPayout = await GetOrCreatePayoutAsync(affiliation.AgencyUserId, currency, periodStart, periodEnd, payouts, ct);
            AddPayoutItem(agencyPayout, p.BookingId.Value, new Money(agencyAmount, currency), Money.Zero(currency), p.Id, ref skipped);
            logger.LogDebug(
                "Agency split for booking {BookingId}: gross={Gross}, platform={Platform}, agency={Agency}, guide={Guide}.",
                p.BookingId.Value,
                grossAmount,
                platformCommission,
                agencyAmount,
                guideAmount);
        }

        foreach (var payout in payouts.Values)
        {
            if (payout.NetAmount.Amount < _options.MinPayoutThreshold)
            {
                logger.LogInformation(
                    "Skipping payout for provider {ProviderId} {Currency}: NetAmount {Net} < threshold {Threshold}.",
                    payout.ProviderId, payout.Currency, payout.NetAmount.Amount, _options.MinPayoutThreshold);
                skipped += payout.PayoutItems.Count;
                continue;
            }

            if (payout.BankAccountId is null)
            {
                var holdResult = payout.PutOnHold("Payout.ProviderPaymentMethodMissing");
                if (holdResult.IsFailure)
                {
                    logger.LogWarning("Failed to hold payout {PayoutId}: {Error}.", payout.Id, holdResult.Errors.FirstOrDefault()?.Message);
                    skipped += payout.PayoutItems.Count;
                    continue;
                }
                onHold++;
            }
            else if (payout.NetAmount.Amount < _options.LargePayoutThreshold)
            {
                var readyResult = payout.MarkReadyForPayout();
                if (readyResult.IsFailure)
                {
                    logger.LogWarning("Failed to ready payout {PayoutId}: {Error}.", payout.Id, readyResult.Errors.FirstOrDefault()?.Message);
                    skipped += payout.PayoutItems.Count;
                    continue;
                }
            }
            // else: keep Pending — admin Approve required

            payout.RaiseScheduled(payout.PayoutItems.Count);
            await payoutRepository.AddAsync(payout, ct);
            created++;
            sweptTotal += payout.NetAmount.Amount;
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Payout trigger completed: created={Created}, skipped={Skipped}, onHold={OnHold}, total={Total} (manual={IsManual}).",
            created, skipped, onHold, sweptTotal, request.IsManual);

        return Result.Success(new TriggerPayoutResult(created, skipped, onHold, sweptTotal));
    }

    private async Task<Payout> GetOrCreatePayoutAsync(
        Guid providerId,
        string currency,
        DateOnly periodStart,
        DateOnly periodEnd,
        Dictionary<(Guid ProviderId, string Currency), Payout> payouts,
        CancellationToken ct)
    {
        var key = (providerId, currency);
        if (payouts.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var method = await paymentMethodRepository.GetDefaultForProviderAsync(providerId, ct);
        var payout = Payout.CreateBatch(providerId, currency, periodStart, periodEnd, method?.Id, timeProvider);
        payouts[key] = payout;
        return payout;
    }

    private void AddPayoutItem(Payout payout, Guid bookingId, Money gross, Money commission, Guid paymentId, ref int skipped)
    {
        var addResult = payout.AddItem(bookingId, gross, commission, null, timeProvider);
        if (addResult.IsFailure)
        {
            logger.LogWarning(
                "Failed to add item for payment {PaymentId} to payout {PayoutId}: {Error}.",
                paymentId,
                payout.Id,
                addResult.Errors.FirstOrDefault()?.Message ?? "Unknown error");
            skipped++;
        }
    }
}


