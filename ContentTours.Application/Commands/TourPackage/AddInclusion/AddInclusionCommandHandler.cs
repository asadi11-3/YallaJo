using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Microsoft.EntityFrameworkCore;

namespace ContentTours.Application.Commands.TourPackage.AddInclusion;

public sealed class AddInclusionCommandHandler(
    ITourPackageRepository repository,
    IDbContext dbContext,
    ICurrentUser currentUser,
    ILogger<AddInclusionCommandHandler> logger)
    : ICommandHandler<AddInclusionCommand>
{
    public async Task<Result> Handle(AddInclusionCommand request, CancellationToken ct)
    {
        logger.LogInformation(
            "Adding inclusion to TourPackage {Id} by User {UserId}",
            request.PackageId,
            currentUser.UserId);

        if (!currentUser.IsAuthenticated)
        {
            return Result.Failure(
                Error.Unauthorized("Authentication required"));
        }

        var package = await repository.GetByIdAsync(request.PackageId, ct);

        if (package is null)
        {
            return Result.Failure(
                new Error("TourPackage.NotFound", "Tour package not found"),
                Outcome.NotFound);
        }

        if (!package.IsActive)
        {
            return Result.Failure(
                new Error("TourPackage.Inactive", "Cannot modify inactive package"));
        }

        if (!currentUser.IsInRole("Admin") &&
            currentUser.UserId != package.Tour.CreatedByUserId)
        {
            return Result.Failure(
                Error.Forbidden("Not allowed"));
        }

        try
        {
            var inclusion = TourPackageInclusion.Create(
                request.PackageId,
                request.Description,
                request.SortOrder);

            package.AddInclusion(inclusion);

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
            "Inclusion added successfully to TourPackage {Id}",
            request.PackageId);

        return Result.Success();
    }
}
