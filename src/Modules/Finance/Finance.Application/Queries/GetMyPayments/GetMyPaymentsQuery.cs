using Finance.Application.Queries.Dtos;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetMyPayments;

/// <summary>
/// GET /payments/my-payments — caller-scoped cursor pagination.
/// </summary>
public sealed record GetMyPaymentsQuery(
    Guid CallerUserId,
    Guid? Cursor,
    int PageSize) : IRequest<Result<PaymentPageDto>>;
