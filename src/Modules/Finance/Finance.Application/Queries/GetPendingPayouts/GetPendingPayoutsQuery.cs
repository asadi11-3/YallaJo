using Finance.Application.Queries.Dtos;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetPendingPayouts;

/// <summary>Admin: list payouts in Pending or Hold status.</summary>
public sealed record GetPendingPayoutsQuery(
    Guid? Cursor,
    int PageSize) : IRequest<Result<PayoutPageDto>>;
