using ContentPlaces.Application.Interfaces;
using ContentPlaces.Application.Queries.BusinessStaff.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using StaffEntity = ContentPlaces.Domain.Entities.BusinessStaff;

namespace ContentPlaces.Application.Commands.BusinessStaff.AddBusinessStaff;

public sealed class AddBusinessStaffCommandHandler(
    IContentPlacesDbContext dbContext,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<AddBusinessStaffCommandHandler> logger)
    : ICommandHandler<AddBusinessStaffCommand, BusinessStaffDto>
{
    public async Task<Result<BusinessStaffDto>> Handle(
        AddBusinessStaffCommand request,
        CancellationToken cancellationToken)
    {
        // check auth
        if (!currentUser.IsAuthenticated)
        {
            return Result<BusinessStaffDto>.Failure(
                new Error("Auth.Unauthorized", "Authentication required"),
                Outcome.Unauthorized);
        }

        // check duplicate
        var exists = await dbContext.BusinessStaff
            .AnyAsync(x =>
                x.BusinessId == request.BusinessId &&
                x.UserId == request.UserId &&
                x.IsActive,
                cancellationToken);

        if (exists)
        {
            return Result<BusinessStaffDto>.Failure(
                new Error("BusinessStaff.Duplicate", "User already added"),
                Outcome.Conflict);
        }

        // create
        var staff = StaffEntity.Create(
            request.BusinessId,
            request.UserId,
            request.Role);

        await dbContext.BusinessStaff.AddAsync(staff, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Staff {StaffId} created: User {UserId} as {Role} in Business {BusinessId} at {Time}",
            staff.Id,
            request.UserId,
            request.Role,
            request.BusinessId,
            DateTime.UtcNow);

        return Result<BusinessStaffDto>.Created(BusinessStaffDto.From(staff));
    }
}
