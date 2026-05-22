using Finance.Application.Queries.Dtos;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetPayoutById;

public sealed record GetPayoutByIdQuery(
    Guid PayoutId,
    Guid CallerUserId,
    bool CallerIsAdmin,
    Guid? CallerProviderId) : IRequest<Result<PayoutDto>>;
