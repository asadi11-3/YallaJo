using Finance.Application.Queries.Dtos;
using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetAdminPayments;

public sealed class GetAdminPaymentsQueryHandler(IPaymentRepository paymentRepository)
    : IRequestHandler<GetAdminPaymentsQuery, Result<PaymentPageDto>>
{
    public async Task<Result<PaymentPageDto>> Handle(GetAdminPaymentsQuery request, CancellationToken ct)
    {
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        pageSize = Math.Clamp(pageSize, 1, 50);
        var (items, nextCursor, totalCount) = await paymentRepository
            .GetAdminPageAsync(
                request.UserId,
                request.ProviderId,
                request.Status,
                request.Type,
                request.Cursor,
                pageSize,
                request.CountTotal,
                ct)
            .ConfigureAwait(false);

        return Result.Success(new PaymentPageDto(
            Items: items.Select(MapToDto).ToList(),
            NextCursor: nextCursor,
            TotalCount: totalCount));
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
