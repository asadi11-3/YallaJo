using Finance.Contracts.Services;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Repositories;
using Finance.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Application.Commands.RefundPayment;

public sealed class RefundPaymentCommandHandler(
    IPaymentRepository paymentRepository,
    IPaymentGateway gateway,
    IFinanceUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<RefundPaymentCommandHandler> logger)
    : IRequestHandler<RefundPaymentCommand, Result<RefundPaymentResult>>
{
    public async Task<Result<RefundPaymentResult>> Handle(RefundPaymentCommand request, CancellationToken ct)
    {
        var original = await paymentRepository.GetByIdAsync(request.PaymentId, ct).ConfigureAwait(false);
        if (original is null)
        {
            return Result.Failure<RefundPaymentResult>(
                new Error("Payment.NotFound", $"Payment {request.PaymentId} not found."),
                Outcome.NotFound);
        }

        if (original.PaymentType != PaymentType.Booking)
        {
            return Result.Failure<RefundPaymentResult>(
                new Error("Refund.PaymentNotCompleted", "Refunds may only target Booking-type payments."),
                Outcome.Conflict);
        }

        if (original.Status != PaymentStatus.Completed
            && original.Status != PaymentStatus.PartiallyRefunded)
        {
            return Result.Failure<RefundPaymentResult>(
                new Error("Refund.PaymentNotCompleted", $"Payment is in status {original.Status}; cannot refund."),
                Outcome.Conflict);
        }

        // F-R11 ownership: caller must be admin, or the buyer, or the provider on this payment.
        var isOwner = request.CallerIsAdmin
            || request.CallerUserId == original.UserId
            || (request.CallerProviderId is not null && request.CallerProviderId.Value == original.ProviderId);
        if (!isOwner)
        {
            return Result.Failure<RefundPaymentResult>(
                new Error("Payment.OwnerMismatch", "Caller does not own this payment."),
                Outcome.Forbidden);
        }

        if (!string.Equals(request.Currency, original.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<RefundPaymentResult>(
                new Error("Refund.CurrencyMismatch", "Refund currency must match the original payment currency."),
                Outcome.Invalid);
        }

        var remaining = original.Amount.Amount - original.RefundedTotal.Amount;
        if (request.Amount > remaining)
        {
            return Result.Failure<RefundPaymentResult>(
                new Error("Refund.AmountExceedsRefundable",
                    $"Refund amount {request.Amount} exceeds refundable balance {remaining}."),
                Outcome.Invalid);
        }

        var refundMoney = new Money(request.Amount, request.Currency.ToUpperInvariant());

        var createRefundResult = original.CreateRefund(refundMoney, request.Reason, original.GatewayProvider, timeProvider);
        if (createRefundResult.IsFailure)
        {
            return Result.Failure<RefundPaymentResult>(
                createRefundResult.Errors.FirstOrDefault() ?? new Error("Refund.Invalid", "Refund could not be created."),
                createRefundResult.Outcome);
        }

        var refund = createRefundResult.Value!;

        await paymentRepository.AddAsync(refund, ct).ConfigureAwait(false);

        // Call gateway eagerly.
        RefundResult? gatewayResult;
        try
        {
            gatewayResult = await gateway.RefundAsync(
                new RefundRequest(
                    OriginalGatewayPaymentId: original.GatewayTransactionId ?? original.TransactionId ?? string.Empty,
                    Amount: request.Amount,
                    Currency: refundMoney.Currency,
                    Reason: request.Reason),
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gateway RefundAsync threw for payment {PaymentId}.", original.Id);
            return Result.Failure<RefundPaymentResult>(
                new Error("Refund.GatewayError", "Gateway refund call failed."),
                Outcome.ServerError);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (gatewayResult.Status == RefundStatus.Completed)
        {
            var stampResult = refund.StampGatewayRefund(gatewayResult.GatewayRefundId);
            if (stampResult.IsFailure)
            {
                return Result.Failure<RefundPaymentResult>(stampResult.Errors.First(), stampResult.Outcome);
            }
            var refundCompleted = refund.MarkRefundCompleted(gatewayResult.GatewayRefundId, nowUtc);
            if (refundCompleted.IsFailure)
            {
                return Result.Failure<RefundPaymentResult>(refundCompleted.Errors.First(), refundCompleted.Outcome);
            }

            var applyResult = original.ApplyRefundCompletion(refundMoney, nowUtc);
            if (applyResult.IsFailure)
            {
                return Result.Failure<RefundPaymentResult>(applyResult.Errors.First(), applyResult.Outcome);
            }
        }
        else if (gatewayResult.Status == RefundStatus.Failed)
        {
            var refundFailed = refund.MarkRefundFailed(gatewayResult.FailureCode ?? "Gateway reported Failed.", nowUtc);
            if (refundFailed.IsFailure)
            {
                return Result.Failure<RefundPaymentResult>(refundFailed.Errors.First(), refundFailed.Outcome);
            }
        }
        // Pending: leave as-is, webhook will finalize.

        try
        {
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("RefundPayment concurrency conflict on payment {PaymentId}.", original.Id);
            return Result.Failure<RefundPaymentResult>(
                new Error("Payment.ConcurrencyConflict", "The payment was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        logger.LogInformation(
            "Refund {RefundId} created for payment {PaymentId} amount={Amount} {Currency} status={Status}.",
            refund.Id, original.Id, request.Amount, refundMoney.Currency, refund.Status);

        return Result.Success(new RefundPaymentResult(
            RefundPaymentId: refund.Id,
            OriginalPaymentId: original.Id,
            Amount: request.Amount,
            Currency: refundMoney.Currency,
            Status: refund.Status.ToString(),
            GatewayRefundId: refund.GatewayTransactionId));
    }
}
