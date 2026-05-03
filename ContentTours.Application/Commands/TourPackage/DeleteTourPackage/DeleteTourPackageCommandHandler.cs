using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using Microsoft.EntityFrameworkCore;

namespace ContentTours.Application.Commands.TourPackage.DeleteTourPackage;

public sealed class DeleteTourPackageCommandHandler(
    ITourPackageRepository repository,
    IDbContext dbContext,
    ICurrentUser currentUser,
    ILogger<DeleteTourPackageCommandHandler> logger)
    : ICommandHandler<DeleteTourPackageCommand>
{
    public async Task<Result> Handle(
        DeleteTourPackageCommand request,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Deleting TourPackage {Id}",
            request.Id);

        if (!currentUser.IsAuthenticated)
        {
            return Result.Failure(
                Error.Unauthorized("Authentication required"));
        }

        var package = await repository.GetByIdAsync(request.Id, ct);

        if (package is null)
        {
            logger.LogWarning(
                "TourPackage {Id} not found",
                request.Id);

            return Result.Failure(
                new Error("TourPackage.NotFound", "Tour package not found"),
                Outcome.NotFound);
        }

        if (!currentUser.IsInRole("Admin") &&
            currentUser.UserId != package.Tour.CreatedByUserId)
        {
            logger.LogWarning(
                "User {UserId} not allowed to delete TourPackage {Id}",
                currentUser.UserId,
                request.Id);

            return Result.Failure(
                Error.Forbidden("Not allowed to delete this package"));
        }

        if (!package.IsActive)
        {
            return Result.Success();
        }

        try
        {
            package.Deactivate();

            await dbContext.SaveChangesAsync(ct);
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
            "TourPackage {Id} deleted successfully",
            request.Id);

        return Result.Success();
    }
}
