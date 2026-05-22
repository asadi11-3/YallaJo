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
                payment.MarkCompleted(request.GatewayPaymentId, request.OccurredAt);
                break;

            case "payment.failed":
                payment.MarkFailed(
                    reasonCode: request.FailureCode ?? "Gateway.Failed",
                    rawReason: request.FailureMessage ?? "Gateway reported failed.",
                    occurredAtUtc: request.OccurredAt);
                break;

            case "refund.succeeded":
                if (payment.PaymentType != PaymentType.Refund)
                {
                    return Result.Failure<ProcessWebhookResult>(
                        new Error("Payment.WebhookEventUnknown", "Refund webhook targets non-refund payment row."),
                        Outcome.Invalid);
                }

                payment.MarkRefundCompleted(request.GatewayPaymentId, request.OccurredAt);

                if (payment.OriginalPaymentId is not null)
                {
                    var original = await paymentRepository
                        .GetByIdAsync(payment.OriginalPaymentId.Value, ct)
                        .ConfigureAwait(false);

                    if (original is not null)
                    {
                        var refundMoney = new Money(Math.Abs(payment.Amount.Amount), payment.Currency);
                        original.ApplyRefundCompletion(refundMoney, request.OccurredAt);
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

                payment.MarkRefundFailed(
                    failureReason: request.FailureMessage ?? request.FailureCode ?? "Gateway reported refund failed.",
                    occurredAtUtc: request.OccurredAt);
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
