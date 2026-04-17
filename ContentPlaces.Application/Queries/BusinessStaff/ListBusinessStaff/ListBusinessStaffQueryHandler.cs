using ContentPlaces.Application.Interfaces;
using ContentPlaces.Application.Queries.BusinessStaff.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.BusinessStaff.ListBusinessStaff;

public sealed class ListBusinessStaffQueryHandler(
    IContentPlacesDbContext dbContext,
    ILogger<ListBusinessStaffQueryHandler> logger)
    : IQueryHandler<ListBusinessStaffQuery, IReadOnlyList<BusinessStaffDto>>
{
    public async Task<Result<IReadOnlyList<BusinessStaffDto>>> Handle(
        ListBusinessStaffQuery request,
        CancellationToken cancellationToken)
    {
        var staff = await dbContext.BusinessStaff
            .AsNoTracking()
            .Where(x => x.BusinessId == request.BusinessId && x.IsActive) // ignore deactivated
            .OrderBy(x => x.Role) // stable order
            .ThenBy(x => x.UserId)
            .Select(x => BusinessStaffDto.From(x))
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "Fetched {Count} staff members for Business {BusinessId}",
            staff.Count,
            request.BusinessId);

        return Result<IReadOnlyList<BusinessStaffDto>>.Success(staff);
    }
}
