using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourPackage.AddInclusion;

public sealed class AddInclusionCommandHandler(
    ITourPackageRepository repository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<AddInclusionCommandHandler> logger)
    : ICommandHandler<AddInclusionCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        AddInclusionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            

            var callerId = currentUser.UserId!.Value;

            var package = await repository
                .GetByIdWithDetailsAsync(request.PackageId, cancellationToken)
                .ConfigureAwait(false);

            if (package is null)
            {
                return Result.Failure<Guid>(
                    new Error("TourPackage.NotFound", $"Tour package '{request.PackageId}' was not found."),
                    Outcome.NotFound);
            }

            if (package.CreatedByUserId != callerId)
            {
                return Result.Failure<Guid>(
                    new Error(
                        "TourPackage.NotOwner",
                        "You do not have permission to modify this tour package."),
                    Outcome.Forbidden);
            }

            var trimmedDescription = (request.Description ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedDescription))
            {
                return Result.Failure<Guid>(
                    new Error(
                        "TourPackageInclusion.InvalidDescription",
                        "Description is required."),
                    Outcome.Invalid);
            }

            // Pre-check duplicate via in-memory loaded inclusions, then defence-in-depth via DB.
            var inMemoryDuplicate = package.Inclusions.Any(i =>
                string.Equals(i.Description, trimmedDescription, StringComparison.OrdinalIgnoreCase));
            if (inMemoryDuplicate
                || await repository.InclusionDescriptionExistsAsync(
                       package.Id, trimmedDescription, cancellationToken).ConfigureAwait(false))
            {
                return Result.Failure<Guid>(
                    new Error(
                        "TourPackageInclusion.Duplicate",
                        "An inclusion with the same description already exists for this package."),
                    Outcome.Conflict);
            }

            var inclusion = package.AddInclusion(trimmedDescription);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex,
                    "Concurrency conflict adding inclusion to TourPackage {PackageId}", package.Id);
                return Result.Failure<Guid>(
                    new Error(
                        "TourPackage.ConcurrencyConflict",
                        "The package was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForPackage(package.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagPackagesList, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Inclusion {InclusionId} added to TourPackage {PackageId} (SortOrder={SortOrder}) by {UserId}",
                inclusion.Id, package.Id, inclusion.SortOrder, callerId);

            return Result.Success(inclusion.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<Guid>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
