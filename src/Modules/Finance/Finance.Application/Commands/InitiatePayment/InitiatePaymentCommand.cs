using Finance.Domain.Enums;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Commands.InitiatePayment;

/// <summary>
/// Initiates a Payment row for an existing booking and calls the configured payment gateway
/// to start the off-platform redirect/charge flow.
/// </summary>
public sealed record InitiatePaymentCommand(
    Guid BookingId,
    PaymentMethod PaymentMethod,
    string ReturnUrl,
    Guid CallerUserId) : IRequest<Result<InitiatePaymentResult>>;

/// <summary>Response payload for POST /payments/initiate.</summary>
public sealed record InitiatePaymentResult(
    Guid PaymentId,
    string GatewayPaymentId,
    string? RedirectUrl,
    string? ClientSecret,
    DateTime? ExpiresAt);
