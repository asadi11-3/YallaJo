using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Exceptions;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.BusinessStaff.RemoveBusinessStaff;

public sealed class RemoveBusinessStaffCommandHandler(
    IBusinessStaffRepository staffRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<RemoveBusinessStaffCommandHandler> logger)
    : ICommandHandler<RemoveBusinessStaffCommand>
{
    public async Task<Result> Handle(
        RemoveBusinessStaffCommand request,
        CancellationToken cancellationToken)
    {
        // Authentication
        if (!currentUser.IsAuthenticated)
        {
            return Result.Failure(
                Error.Unauthorized("Authentication required"));
        }

        var staff = await staffRepository.GetByIdAsync(
            request.Id,
            cancellationToken,
            asNoTracking: false);

        if (staff is null)
        {
            return Result.Failure(
                Error.NotFound("BusinessStaff.NotFound", "Staff not found"));
        }

        var isAdmin = currentUser.IsInRole("Admin");

        if (!isAdmin && staff.Business.OwnerId != currentUser.UserId)
        {
            return Result.Failure(
                Error.Forbidden("You are not allowed to modify this business"));
        }

        // already inactive
        if (!staff.IsActive)
        {
            return Result.Failure(
                Error.Conflict("BusinessStaff.AlreadyInactive", "Staff already deactivated"));
        }

        // deactivate
        staff.Deactivate();

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ContentPlaceConcurrencyException)
        {
            return Result.Failure(
                Error.Conflict(
                    "BusinessStaff.ConcurrencyConflict",
                    "A concurrency conflict occurred. Please refresh and try again."));
        }

        logger.LogInformation("Staff {StaffId} deactivated", request.Id);

        return Result.Success();
    }
}
