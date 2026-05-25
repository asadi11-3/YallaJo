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
    IProviderBankAccountRepository bankAccountRepository,
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

        // Group by (ProviderId, Currency)
        var groups = eligible.GroupBy(p => (p.ProviderId, p.Currency));
        var created = 0;
        var skipped = 0;
        var onHold = 0;
        decimal sweptTotal = 0m;

        foreach (var group in groups)
        {
            var providerId = group.Key.ProviderId;
            var currency = group.Key.Currency;
            var members = group.ToList();

            // Resolve bank account (must be verified for currency)
            var bank = await bankAccountRepository.GetDefaultForProviderAsync(providerId, currency, ct);
            Guid? bankAccountId = bank?.Id;

            var payout = Payout.CreateBatch(providerId, currency, periodStart, periodEnd, bankAccountId, timeProvider);

            foreach (var p in members)
            {
                if (!p.BookingId.HasValue)
                {
                    skipped++;
                    continue;
                }

                // Look up commission rate per booking. Stub returns 10% in current shape;
                // T6 reshape will switch this to tier-based lookup keyed by provider + currency.
                decimal commissionRate;
                try
                {
                    var lookupResult = await commissionLookup.GetCommissionAsync(p.BookingId!.Value, ct);
                    commissionRate = lookupResult.Rate;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Commission lookup failed for payment {PaymentId}; defaulting to 0.15.", p.Id);
                    commissionRate = 0.15m;
                }

                var gross = new Money(p.Amount.Amount, currency);
                var commissionAmt = Math.Round(p.Amount.Amount * commissionRate, 2, MidpointRounding.ToEven);
                var commission = new Money(commissionAmt, currency);

                try
                {
                    payout.AddItem(p.BookingId.Value, gross, commission, null, timeProvider);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to add item for payment {PaymentId} to payout {PayoutId}.", p.Id, payout.Id);
                    skipped++;
                }
            }

            // Decide lifecycle
            if (payout.NetAmount.Amount < _options.MinPayoutThreshold)
            {
                // Skip: below threshold, don't persist
                logger.LogInformation(
                    "Skipping payout for provider {ProviderId} {Currency}: NetAmount {Net} < threshold {Threshold}.",
                    providerId, currency, payout.NetAmount.Amount, _options.MinPayoutThreshold);
                skipped += payout.PayoutItems.Count;
                continue;
            }

            if (bank is null)
            {
                payout.PutOnHold("Payout.ProviderBankAccountMissing");
                onHold++;
            }
            else if (payout.NetAmount.Amount < _options.LargePayoutThreshold)
            {
                payout.MarkReadyForPayout();
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
}


