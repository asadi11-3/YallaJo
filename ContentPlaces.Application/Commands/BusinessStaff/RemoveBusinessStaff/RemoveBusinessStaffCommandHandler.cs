using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.BusinessStaff.RemoveBusinessStaff;

public sealed class RemoveBusinessStaffCommandHandler(
    IBusinessStaffRepository staffRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ILogger<RemoveBusinessStaffCommandHandler> logger)
    : ICommandHandler<RemoveBusinessStaffCommand>
{
    public async Task<Result> Handle(
        RemoveBusinessStaffCommand request,
        CancellationToken cancellationToken)
    {
        var staff = await staffRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);

        if (staff is null)
        {
            return Result.Failure(
                new Error("BusinessStaff.NotFound", "Staff not found"),
                Outcome.NotFound);
        }

        if (!staff.IsActive)
        {
            return Result.Failure(
                new Error("BusinessStaff.AlreadyInactive", "Staff already deactivated"),
                Outcome.Conflict);
        }

        staff.Deactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Staff {StaffId} deactivated", request.Id);

        return Result.Success();
    }
}
