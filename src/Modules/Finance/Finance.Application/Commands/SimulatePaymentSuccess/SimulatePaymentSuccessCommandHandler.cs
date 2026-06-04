using Finance.Application.Interfaces;
using Finance.Domain.Enums;
using Finance.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Commands.SimulatePaymentSuccess;

internal sealed class SimulatePaymentSuccessCommandHandler(
    IPaymentExpectationRepository expectations,
    IPaymentRepository payments,
    IFinanceUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<SimulatePaymentSuccessCommandHandler> logger)
    : IRequestHandler<SimulatePaymentSuccessCommand, Result<SimulatePaymentSuccessResult>>
{
    public async Task<Result<SimulatePaymentSuccessResult>> Handle(
        SimulatePaymentSuccessCommand request,
        CancellationToken ct)
    {
        // 1. Booking must have a payment expectation (seeded from tour-booking.created).
        var expectation = await expectations.GetByBookingIdAsync(request.BookingId, ct).ConfigureAwait(false);
        if (expectation is null)
        {
            return Result.Failure<SimulatePaymentSuccessResult>(
                new Error("Payment.BookingNotEligible", "No payment expectation exists for this booking."),
                Outcome.Invalid);
        }

        // 2. Ownership: only the booking owner may simulate their own payment.
        if (expectation.UserId != request.CallerUserId)
        {
            return Result.Failure<SimulatePaymentSuccessResult>(
                new Error("Payment.OwnerMismatch", "Only the booking owner may complete payment."),
                Outcome.Forbidden);
        }

        // 3. Find the in-flight Pending Booking payment (created by /payments/initiate).
        var bookingPayments = await payments.GetByBookingIdAsync(request.BookingId, ct).ConfigureAwait(false);

        var alreadyCompleted = bookingPayments.FirstOrDefault(p =>
            p.PaymentType == PaymentType.Booking && p.Status == PaymentStatus.Completed);
        if (alreadyCompleted is not null)
        {
            // Idempotent: payment already completed.
            return Result.Success(new SimulatePaymentSuccessResult(
                alreadyCompleted.Id, alreadyCompleted.Status.ToString()));
        }

        var pendingId = bookingPayments
            .Where(p => p.PaymentType == PaymentType.Booking
                && p.Status is PaymentStatus.Pending or PaymentStatus.Processing)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefault();

        if (pendingId is null)
        {
            return Result.Failure<SimulatePaymentSuccessResult>(
                new Error("Payment.NotInitiated", "Initiate payment first."),
                Outcome.Conflict);
        }

        var gatewayTxnId = bookingPayments
            .First(p => p.Id == pendingId.Value)
            .GatewayTransactionId;

        var pending = string.IsNullOrEmpty(gatewayTxnId)
            ? await payments.GetByIdAsync(pendingId.Value, ct, asNoTracking: false).ConfigureAwait(false)
            : await payments.GetByGatewayTransactionIdAsync(gatewayTxnId, ct).ConfigureAwait(false);

        if (pending is null)
        {
            return Result.Failure<SimulatePaymentSuccessResult>(
                new Error("Payment.NotInitiated", "Initiate payment first."),
                Outcome.Conflict);
        }

        var effectiveTxnId = pending.GatewayTransactionId ?? $"gw-pay-{pending.Id:N}";
        var completeResult = pending.MarkCompleted(effectiveTxnId, timeProvider.GetUtcNow().UtcDateTime);
        if (completeResult.IsFailure)
        {
            return Result.Failure<SimulatePaymentSuccessResult>(completeResult.Errors.First(), completeResult.Outcome);
        }

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Finance (DEV): simulated payment success for booking {BookingId}, payment {PaymentId}.",
            request.BookingId, pending.Id);

        return Result.Success(new SimulatePaymentSuccessResult(pending.Id, pending.Status.ToString()));
    }
}
