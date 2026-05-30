using Finance.Application.Queries.Dtos;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetPaymentById;

/// <summary>
/// GET /payments/{id} — viewable by buyer, provider (if owner), or admin.
/// </summary>
public sealed record GetPaymentByIdQuery(
    Guid PaymentId,
    Guid CallerUserId,
    bool CallerIsAdmin,
    Guid? CallerProviderId) : IRequest<Result<PaymentDto>>;
