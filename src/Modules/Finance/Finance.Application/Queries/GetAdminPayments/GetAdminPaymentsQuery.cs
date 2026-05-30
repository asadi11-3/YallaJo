using Finance.Application.Queries.Dtos;
using Finance.Domain.Enums;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetAdminPayments;

/// <summary>
/// GET /payments/admin/all — Admin filter + paginate.
/// </summary>
public sealed record GetAdminPaymentsQuery(
    Guid? UserId,
    Guid? ProviderId,
    PaymentStatus? Status,
    PaymentType? Type,
    Guid? Cursor,
    int PageSize,
    bool CountTotal) : IRequest<Result<PaymentPageDto>>;
