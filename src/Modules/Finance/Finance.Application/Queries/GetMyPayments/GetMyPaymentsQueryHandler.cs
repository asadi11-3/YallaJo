using Finance.Application.Queries.Dtos;
using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetMyPayments;

public sealed class GetMyPaymentsQueryHandler(IPaymentRepository paymentRepository)
    : IRequestHandler<GetMyPaymentsQuery, Result<PaymentPageDto>>
{
    public async Task<Result<PaymentPageDto>> Handle(GetMyPaymentsQuery request, CancellationToken ct)
    {
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        pageSize = Math.Clamp(pageSize, 1, 50);
        var (items, nextCursor) = await paymentRepository
            .GetMyPaymentsPageAsync(request.CallerUserId, request.Cursor, pageSize, ct)
            .ConfigureAwait(false);

        return Result.Success(new PaymentPageDto(
            Items: items.Select(MapToDto).ToList(),
            NextCursor: nextCursor,
            TotalCount: null));
    }

    private static PaymentDto MapToDto(Payment p) => new(
        Id: p.Id,
        UserId: p.UserId,
        ProviderId: p.ProviderId,
        BookingId: p.BookingId,
        OriginalPaymentId: p.OriginalPaymentId,
        Amount: p.Amount.Amount,
        Currency: p.Currency,
        PaymentMethod: p.PaymentMethod.ToString(),
        PaymentType: p.PaymentType.ToString(),
        Status: p.Status.ToString(),
        GatewayProvider: p.GatewayProvider,
        GatewayTransactionId: p.GatewayTransactionId,
        RedirectUrl: p.RedirectUrl?.ToString(),
        RecipientAccount: p.RecipientAccount,
        PaidAt: p.PaidAt,
        ExpiresAt: p.ExpiresAt,
        EscrowReleaseEligibleAt: p.EscrowReleaseEligibleAt,
        RefundedTotal: p.RefundedTotal.Amount,
        RetryCount: p.RetryCount,
        CreatedAt: p.CreatedAt,
        UpdatedAt: p.UpdatedAt);
}
