using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using Microsoft.EntityFrameworkCore;

namespace ContentTours.Application.Commands.TourPackage.UpdateTourPackage;

public sealed class UpdateTourPackageCommandHandler(
    ITourPackageRepository packageRepository,
    IDbContext dbContext,
    ICurrentUser currentUser,
    ILogger<UpdateTourPackageCommandHandler> logger)
    : ICommandHandler<UpdateTourPackageCommand>
{
    public async Task<Result> Handle(
        UpdateTourPackageCommand request,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Updating TourPackage {Id}",
            request.Id);

        if (!currentUser.IsAuthenticated)
        {
            return Result.Failure(
                Error.Unauthorized("Authentication required"));
        }

        var package = await packageRepository.GetByIdAsync(request.Id, ct);

        if (package is null)
        {
            logger.LogWarning(
                "TourPackage {Id} not found",
                request.Id);

            return Result.Failure(
                new Error("TourPackage.NotFound", "Tour package not found"),
                Outcome.NotFound);
        }

        if (!package.IsActive)
        {
            return Result.Failure(
                new Error("TourPackage.Inactive", "Cannot update inactive package"));
        }

        if (!currentUser.IsInRole("Admin") &&
            currentUser.UserId != package.Tour.CreatedByUserId)
        {
            logger.LogWarning(
                "User {UserId} not allowed to update TourPackage {Id}",
                currentUser.UserId,
                request.Id);

            return Result.Failure(
                Error.Forbidden("Not allowed to update this package"));
        }

        try
        {
            package.Update(
                request.Name,
                request.Description,
                request.Price,
                request.Currency,
                request.MaxParticipants,
                request.ValidTo
            );

            await dbContext.SaveChangesAsync(ct);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(
                new Error("TourPackage.Invalid", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(
                new Error("TourPackage.Invalid", ex.Message));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("TourPackage.ConcurrencyConflict", "Concurrency conflict"),
                Outcome.Conflict);
        }

        logger.LogInformation(
            "TourPackage {Id} updated successfully",
            request.Id);

        return Result.Success();
    }
}
