using ContentPlaces.Application.Queries.BusinessStaff.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.BusinessStaff.ListBusinessStaff;

public sealed class ListBusinessStaffQueryHandler(
    IBusinessStaffRepository staffRepository,
    ILogger<ListBusinessStaffQueryHandler> logger)
    : IQueryHandler<ListBusinessStaffQuery, IReadOnlyList<BusinessStaffDto>>
{
    public async Task<Result<IReadOnlyList<BusinessStaffDto>>> Handle(
        ListBusinessStaffQuery request,
        CancellationToken cancellationToken)
    {
        var staff = await staffRepository.SelectAsync(
            selector: x => BusinessStaffDto.From(x),
            filter: x => x.BusinessId == request.BusinessId && x.IsActive,
            orderBy: q => q.OrderBy(x => x.Role).ThenBy(x => x.UserId),
            ct: cancellationToken);

        logger.LogInformation(
            "Fetched {Count} staff members for Business {BusinessId}",
            staff.Count, request.BusinessId);

        return Result<IReadOnlyList<BusinessStaffDto>>.Success(staff);
    }
}
