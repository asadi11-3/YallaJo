using Finance.Application.Interfaces;
using Finance.Contracts.Services;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Application.Commands.InitiatePayment;

/// <summary>
/// Handles POST /payments/initiate: creates a Pending Payment row, calls the gateway,
/// stamps the gateway response, and marks the booking's PaymentExpectation as Paid.
/// Idempotent: a repeated call for a booking with an existing Pending Payment returns that row.
/// </summary>
internal sealed class InitiatePaymentCommandHandler(
    IPaymentExpectationRepository expectations,
    IPaymentRepository payments,
    IPaymentGateway gateway,
    IFinanceUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<InitiatePaymentCommandHandler> logger)
    : IRequestHandler<InitiatePaymentCommand, Result<InitiatePaymentResult>>
{
    public async Task<Result<InitiatePaymentResult>> Handle(
        InitiatePaymentCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Lookup booking expectation seeded by booking.tour-booking.created.v1.
        var expectation = await expectations.GetByBookingIdAsync(request.BookingId, ct).ConfigureAwait(false);
        if (expectation is null)
        {
            return Result.Failure<InitiatePaymentResult>(
                new Error(
                    "Payment.BookingNotEligible",
                    "No payment expectation exists for this booking. The booking may not yet have been propagated."),
                Outcome.Invalid);
        }

        // 2. Ownership check (F-R11): the caller must be the booking owner.
        if (expectation.UserId != request.CallerUserId)
        {
            return Result.Failure<InitiatePaymentResult>(
                new Error(
                    "Payment.OwnerMismatch",
                    "Only the booking owner may initiate payment."),
                Outcome.Forbidden);
        }

        // 3. Lifecycle guard: only AwaitingPayment expectations may be paid.
        if (expectation.Status != PaymentExpectationStatus.AwaitingPayment)
        {
            // 3a. Idempotency: if already Paid, return the existing Payment row.
            if (expectation.Status == PaymentExpectationStatus.Paid && expectation.PaymentId is Guid existingId)
            {
                var existing = await payments.GetByIdAsync(existingId, ct).ConfigureAwait(false);
                if (existing is not null)
                {
                    return Result.Success(MapResponse(existing));
                }
            }

            return Result.Failure<InitiatePaymentResult>(
                new Error(
                    "Payment.AlreadyInitiated",
                    "This booking is not eligible for payment initiation."),
                Outcome.Conflict);
        }

        // 4. Defensive idempotency: scan Payment by bookingId for in-flight Pending row.
        var existingPayments = await payments.GetByBookingIdAsync(request.BookingId, ct).ConfigureAwait(false);
        var existingPending = existingPayments.FirstOrDefault(p =>
            p.Status == PaymentStatus.Pending
            && p.PaymentType == PaymentType.Booking);
        if (existingPending is not null)
        {
            return Result.Success(MapResponse(existingPending));
        }

        // 5. Build the new Payment aggregate. Money carries the canonical amount + currency.
        var amount = new Money(expectation.ExpectedAmount.Amount, expectation.Currency);
        var payment = Payment.Initiate(
            bookingId: expectation.BookingId,
            userId: expectation.UserId,
            providerId: expectation.ProviderId,
            amount: amount,
            paymentMethod: request.PaymentMethod,
            gatewayProvider: gateway.GatewayName,
            timeProvider: timeProvider);

        // 6. Call the gateway to initiate the off-platform charge flow.
        var gatewayRequest = new InitiateRequest(
            PaymentId: payment.Id,
            Amount: amount.Amount,
            Currency: amount.Currency,
            DescribedAs: $"Booking {expectation.Reference}",
            ReturnUrl: new Uri(request.ReturnUrl, UriKind.Absolute),
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bookingId"] = expectation.BookingId.ToString(),
                ["userId"] = expectation.UserId.ToString(),
                ["reference"] = expectation.Reference,
            });

        InitiateResult gatewayResult;
        try
        {
            gatewayResult = await gateway.InitiateAsync(gatewayRequest, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Finance: gateway {GatewayName} threw while initiating payment for booking {BookingId}.",
                gateway.GatewayName, expectation.BookingId);

            return Result.Failure<InitiatePaymentResult>(
                new Error("Payment.GatewayError", $"Gateway initiation failed: {ex.Message}"),
                Outcome.ServerError);
        }

        if (string.IsNullOrWhiteSpace(gatewayResult.GatewayPaymentId))
        {
            return Result.Failure<InitiatePaymentResult>(
                new Error(
                    "Payment.GatewayError",
                    "Gateway did not return a payment identifier."),
                Outcome.ServerError);
        }

        payment.StampGatewayInitiation(
            gatewayResult.GatewayPaymentId,
            gatewayResult.RedirectUrl,
            gatewayResult.ClientSecret);

        // 7. Persist Payment + flip expectation -> Paid in one UoW.
        await payments.AddAsync(payment, ct).ConfigureAwait(false);
        expectation.MarkPaid(payment.Id);

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Finance: Payment {PaymentId} initiated for booking {BookingId} via gateway {GatewayName} (gw txn {GatewayTxnId}).",
            payment.Id, expectation.BookingId, gateway.GatewayName, gatewayResult.GatewayPaymentId);

        return Result.Success(MapResponse(payment));
    }

    private static InitiatePaymentResult MapResponse(Payment payment) => new(
        PaymentId: payment.Id,
        GatewayPaymentId: payment.GatewayTransactionId ?? string.Empty,
        RedirectUrl: payment.RedirectUrl?.ToString(),
        ClientSecret: payment.ClientSecret,
        ExpiresAt: payment.ExpiresAt);
}
