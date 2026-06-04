using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Commands.SimulatePaymentSuccess;

public sealed record SimulatePaymentSuccessCommand(
    Guid BookingId,
    Guid CallerUserId) : IRequest<Result<SimulatePaymentSuccessResult>>;

public sealed record SimulatePaymentSuccessResult(
    Guid PaymentId,
    string PaymentStatus);
