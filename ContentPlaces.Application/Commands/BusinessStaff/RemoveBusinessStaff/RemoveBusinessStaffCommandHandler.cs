using ContentPlaces.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.BusinessStaff.RemoveBusinessStaff;

public sealed class RemoveBusinessStaffCommandHandler(
    IContentPlacesDbContext dbContext,
    IContentPlacesUnitOfWork unitOfWork,
    ILogger<RemoveBusinessStaffCommandHandler> logger)
    : ICommandHandler<RemoveBusinessStaffCommand>
{
    public async Task<Result> Handle(
        RemoveBusinessStaffCommand request,
        CancellationToken cancellationToken)
    {
        var staff = await dbContext.BusinessStaff
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (staff is null)
        {
            return Result.Failure(
                new Error("BusinessStaff.NotFound", "Staff not found"),
                Outcome.NotFound);
        }

        // بيمنع duplicate deactivation
        if (!staff.IsActive)
        {
            return Result.Failure(
                new Error("BusinessStaff.AlreadyInactive", "Staff already deactivated"),
                Outcome.Conflict);
        }

        // soft delete
        staff.Deactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Staff {StaffId} deactivated at {Time}",
            request.Id,
            DateTime.UtcNow);

        return Result.Success();
    }
}
