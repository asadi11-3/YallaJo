using ContentPlaces.Application.Interfaces;
using ContentPlaces.Application.Queries.BusinessStaff.Common;
using ContentPlaces.Domain.Exceptions;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using StaffEntity = ContentPlaces.Domain.Entities.BusinessStaff;

namespace ContentPlaces.Application.Commands.BusinessStaff.AddBusinessStaff;

public sealed class AddBusinessStaffCommandHandler(
    IBusinessStaffRepository staffRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<AddBusinessStaffCommandHandler> logger)
    : ICommandHandler<AddBusinessStaffCommand, BusinessStaffDto>
{
    public async Task<Result<BusinessStaffDto>> Handle(
        AddBusinessStaffCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return Result<BusinessStaffDto>.Failure(
                new Error("Auth.Unauthorized", "Authentication required"),
                Outcome.Unauthorized);
        }

        var exists = await staffRepository.AnyAsync(
            x => x.BusinessId == request.BusinessId &&
                 x.UserId == request.UserId &&
                 x.IsActive,
            cancellationToken);

        if (exists)
        {
            return Result<BusinessStaffDto>.Failure(
                new Error("BusinessStaff.Duplicate", "User already added"),
                Outcome.Conflict);
        }

        var staff = StaffEntity.Create(
            request.BusinessId,
            request.UserId,
            request.Role);

        await staffRepository.AddAsync(staff, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ContentPlaceConcurrencyException)
        {
            return Result<BusinessStaffDto>.Failure(
                new Error(
                    "BusinessStaff.ConcurrencyConflict",
                    "A concurrency conflict occurred. Please refresh and try again."),
                Outcome.Conflict);
        }

        logger.LogInformation(
            "Staff {StaffId} created: User {UserId} as {Role} in Business {BusinessId}",
            staff.Id, request.UserId, request.Role, request.BusinessId);

        return Result<BusinessStaffDto>.Created(BusinessStaffDto.From(staff));
    }
}
