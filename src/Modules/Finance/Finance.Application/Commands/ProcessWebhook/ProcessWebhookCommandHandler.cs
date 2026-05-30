using System.Security.Cryptography;
using System.Text;
using Finance.Application.Interfaces;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Application.Commands.ProcessWebhook;

/// <summary>
/// Processes a normalized webhook envelope. F-R3 idempotency: the inbox de-duplicates by EventId.
/// Dispatches on EventType ∈ {payment.succeeded, payment.failed, refund.succeeded, refund.failed}.
/// </summary>
public sealed class ProcessWebhookCommandHandler(
    IPaymentRepository paymentRepository,
    IFinanceInboxStore inboxStore,
    IFinanceUnitOfWork unitOfWork,
    ILogger<ProcessWebhookCommandHandler> logger)
    : IRequestHandler<ProcessWebhookCommand, Result<ProcessWebhookResult>>
{
    public async Task<Result<ProcessWebhookResult>> Handle(ProcessWebhookCommand request, CancellationToken ct)
    {
        var messageId = DeriveMessageId(request.EventId);

        if (await inboxStore.HasBeenProcessedAsync(messageId, ct).ConfigureAwait(false))
        {
            logger.LogInformation(
                "Finance webhook: EventId={EventId} (MessageId={MessageId}) already processed — replay idempotent.",
                request.EventId, messageId);
            return Result.Success(new ProcessWebhookResult(Idempotent: true, PaymentId: null, PaymentStatus: null));
        }

        var payment = await paymentRepository
            .GetByGatewayTransactionIdAsync(request.GatewayPaymentId, ct)
            .ConfigureAwait(false);

        if (payment is null)
        {
            logger.LogWarning(
                "Finance webhook: GatewayPaymentId={GatewayPaymentId} not found; will retry later.",
                request.GatewayPaymentId);
            return Result.Failure<ProcessWebhookResult>(
                new Error("Payment.NotFound", "Payment not found for the given gateway id."),
                Outcome.NotFound);
        }

        switch (request.EventType.ToLowerInvariant())
        {
            case "payment.succeeded":
                var completeResult = payment.MarkCompleted(request.GatewayPaymentId, request.OccurredAt);
                if (completeResult.IsFailure)
                {
                    return Result.Failure<ProcessWebhookResult>(completeResult.Errors.First(), completeResult.Outcome);
                }
                break;

            case "payment.failed":
                var failResult = payment.MarkFailed(
                    reasonCode: request.FailureCode ?? "Gateway.Failed",
                    rawReason: request.FailureMessage ?? "Gateway reported failed.",
                    occurredAtUtc: request.OccurredAt);
                if (failResult.IsFailure)
                {
                    return Result.Failure<ProcessWebhookResult>(failResult.Errors.First(), failResult.Outcome);
                }
                break;

            case "refund.succeeded":
                if (payment.PaymentType != PaymentType.Refund)
                {
                    return Result.Failure<ProcessWebhookResult>(
                        new Error("Payment.WebhookEventUnknown", "Refund webhook targets non-refund payment row."),
                        Outcome.Invalid);
                }

                var refundCompleteResult = payment.MarkRefundCompleted(request.GatewayPaymentId, request.OccurredAt);
                if (refundCompleteResult.IsFailure)
                {
                    return Result.Failure<ProcessWebhookResult>(refundCompleteResult.Errors.First(), refundCompleteResult.Outcome);
                }

                if (payment.OriginalPaymentId is not null)
                {
                    var original = await paymentRepository
                        .GetByIdAsync(payment.OriginalPaymentId.Value, ct)
                        .ConfigureAwait(false);

                    if (original is not null)
                    {
                        var refundMoney = new Money(Math.Abs(payment.Amount.Amount), payment.Currency);
                        var applyResult = original.ApplyRefundCompletion(refundMoney, request.OccurredAt);
                        if (applyResult.IsFailure)
                        {
                            return Result.Failure<ProcessWebhookResult>(applyResult.Errors.First(), applyResult.Outcome);
                        }
                    }
                }
                break;

            case "refund.failed":
                if (payment.PaymentType != PaymentType.Refund)
                {
                    return Result.Failure<ProcessWebhookResult>(
                        new Error("Payment.WebhookEventUnknown", "Refund.failed webhook targets non-refund payment row."),
                        Outcome.Invalid);
                }

                var refundFailResult = payment.MarkRefundFailed(
                    failureReason: request.FailureMessage ?? request.FailureCode ?? "Gateway reported refund failed.",
                    occurredAtUtc: request.OccurredAt);
                if (refundFailResult.IsFailure)
                {
                    return Result.Failure<ProcessWebhookResult>(refundFailResult.Errors.First(), refundFailResult.Outcome);
                }
                break;

            default:
                return Result.Failure<ProcessWebhookResult>(
                    new Error("Payment.WebhookEventUnknown", $"Unknown webhook event type {request.EventType}."),
                    Outcome.Invalid);
        }

        inboxStore.MarkAsProcessed(messageId);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success(new ProcessWebhookResult(
            Idempotent: false,
            PaymentId: payment.Id,
            PaymentStatus: payment.Status.ToString()));
    }

    /// <summary>
    /// Derive a stable Guid messageId from a gateway-supplied EventId string so the inbox de-duplicates by event.
    /// </summary>
    private static Guid DeriveMessageId(string eventId)
    {
        if (Guid.TryParse(eventId, out var parsed))
        {
            return parsed;
        }

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes("finance.webhook:" + eventId));
        var sliced = new byte[16];
        Array.Copy(hash, sliced, 16);
        return new Guid(sliced);
    }
}
