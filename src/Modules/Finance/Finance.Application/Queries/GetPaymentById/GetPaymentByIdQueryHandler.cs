using Finance.Application.Queries.Dtos;
using Finance.Domain.Repositories;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetPaymentById;

public sealed class GetPaymentByIdQueryHandler(IPaymentRepository paymentRepository)
    : IRequestHandler<GetPaymentByIdQuery, Result<PaymentDto>>
{
    public async Task<Result<PaymentDto>> Handle(GetPaymentByIdQuery request, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByIdAsync(request.PaymentId, ct).ConfigureAwait(false);
        if (payment is null)
        {
            return Result.Failure<PaymentDto>(
                new Error("Payment.NotFound", $"Payment {request.PaymentId} not found."),
                Outcome.NotFound);
        }

        var isOwner = request.CallerIsAdmin
            || request.CallerUserId == payment.UserId
            || (request.CallerProviderId is not null && request.CallerProviderId.Value == payment.ProviderId);
        if (!isOwner)
        {
            return Result.Failure<PaymentDto>(
                new Error("Payment.OwnerMismatch", "Caller does not own this payment."),
                Outcome.Forbidden);
        }

        return Result.Success(new PaymentDto(
            Id: payment.Id,
            UserId: payment.UserId,
            ProviderId: payment.ProviderId,
            BookingId: payment.BookingId,
            OriginalPaymentId: payment.OriginalPaymentId,
            Amount: payment.Amount.Amount,
            Currency: payment.Currency,
            PaymentMethod: payment.PaymentMethod.ToString(),
            PaymentType: payment.PaymentType.ToString(),
            Status: payment.Status.ToString(),
            GatewayProvider: payment.GatewayProvider,
            GatewayTransactionId: payment.GatewayTransactionId,
            RedirectUrl: payment.RedirectUrl?.ToString(),
            RecipientAccount: payment.RecipientAccount,
            PaidAt: payment.PaidAt,
            ExpiresAt: payment.ExpiresAt,
            EscrowReleaseEligibleAt: payment.EscrowReleaseEligibleAt,
            RefundedTotal: payment.RefundedTotal.Amount,
            RetryCount: payment.RetryCount,
            CreatedAt: payment.CreatedAt,
            UpdatedAt: payment.UpdatedAt));
    }
}
