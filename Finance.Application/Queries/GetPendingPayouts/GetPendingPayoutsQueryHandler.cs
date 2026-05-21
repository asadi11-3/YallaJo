using Finance.Application.Queries.Dtos;
using Finance.Application.Queries.GetPayoutById;
using Finance.Domain.Repositories;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetPendingPayouts;

public sealed class GetPendingPayoutsQueryHandler(IPayoutRepository payoutRepository)
    : IRequestHandler<GetPendingPayoutsQuery, Result<PayoutPageDto>>
{
    public async Task<Result<PayoutPageDto>> Handle(GetPendingPayoutsQuery request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var pageSize = request.PageSize;
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, nextCursor) = await payoutRepository.GetPendingApprovalAsync(request.Cursor, pageSize, ct);
        var dtos = items.Select(GetPayoutByIdQueryHandler.MapToDto).ToList();
        return Result.Success(new PayoutPageDto(dtos, nextCursor));
    }
}
