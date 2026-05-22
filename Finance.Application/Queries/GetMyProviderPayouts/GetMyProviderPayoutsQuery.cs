using Finance.Application.Queries.Dtos;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetMyProviderPayouts;

/// <summary>Provider self-view: payouts where ProviderId == caller's provider claim.</summary>
public sealed record GetMyProviderPayoutsQuery(
    Guid ProviderId,
    Guid? Cursor,
    int PageSize) : IRequest<Result<PayoutPageDto>>;
