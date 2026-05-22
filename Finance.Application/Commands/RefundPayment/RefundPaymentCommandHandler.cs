using Finance.Contracts.Services;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Repositories;
using Finance.Application.Interfaces;
using MediatR;
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

        Payment refund;
        try
        {
            refund = original.CreateRefund(refundMoney, request.Reason, original.GatewayProvider, timeProvider);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<RefundPaymentResult>(
                new Error("Refund.PaymentNotCompleted", ex.Message),
                Outcome.Conflict);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<RefundPaymentResult>(
                new Error("Refund.AmountExceedsRefundable", ex.Message),
                Outcome.Invalid);
        }

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
            refund.StampGatewayRefund(gatewayResult.GatewayRefundId);
            refund.MarkRefundCompleted(gatewayResult.GatewayRefundId, nowUtc);
            original.ApplyRefundCompletion(refundMoney, nowUtc);
        }
        else if (gatewayResult.Status == RefundStatus.Failed)
        {
            refund.MarkRefundFailed(gatewayResult.FailureCode ?? "Gateway reported Failed.", nowUtc);
        }
        // Pending: leave as-is, webhook will finalize.

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

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
