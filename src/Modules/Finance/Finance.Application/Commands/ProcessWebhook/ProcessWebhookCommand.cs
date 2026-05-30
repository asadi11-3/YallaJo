using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Commands.ProcessWebhook;

/// <summary>
/// Webhook payload after HMAC verification has already passed at the endpoint layer.
/// Body shape is gateway-normalized at this point.
/// </summary>
public sealed record ProcessWebhookCommand(
    string EventId,
    string EventType,
    DateTime OccurredAt,
    string GatewayPaymentId,
    decimal Amount,
    string Currency,
    string? FailureCode,
    string? FailureMessage,
    string Source) : IRequest<Result<ProcessWebhookResult>>;

public sealed record ProcessWebhookResult(
    bool Idempotent,
    Guid? PaymentId,
    string? PaymentStatus);
