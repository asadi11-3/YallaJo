using Finance.Application.Queries.Dtos;
using Finance.Application.Queries.GetPayoutById;
using Finance.Domain.Repositories;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetMyProviderPayouts;

public sealed class GetMyProviderPayoutsQueryHandler(IPayoutRepository payoutRepository)
    : IRequestHandler<GetMyProviderPayoutsQuery, Result<PayoutPageDto>>
{
    public async Task<Result<PayoutPageDto>> Handle(GetMyProviderPayoutsQuery request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var pageSize = request.PageSize;
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, nextCursor) = await payoutRepository.GetByProviderAsync(request.ProviderId, request.Cursor, pageSize, ct);
        var dtos = items.Select(GetPayoutByIdQueryHandler.MapToDto).ToList();
        return Result.Success(new PayoutPageDto(dtos, nextCursor));
    }
}
