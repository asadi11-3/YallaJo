using ContentTours.Application.Interfaces;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Microsoft.EntityFrameworkCore;

using TourPackageEntity = ContentTours.Domain.Entities.TourPackage;

namespace ContentTours.Application.Commands.TourPackage.CreateTourPackage;

public sealed class CreateTourPackageCommandHandler(
    ITourPackageRepository packageRepository,
    ITourRepository tourRepository,
    IContentToursUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<CreateTourPackageCommandHandler> logger)
    : ICommandHandler<CreateTourPackageCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateTourPackageCommand request,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Creating TourPackage for TourId {TourId} by User {UserId}",
            request.TourId,
            currentUser.UserId);

        if (!currentUser.IsAuthenticated)
        {
            return Result.Failure<Guid>(
                Error.Unauthorized("Authentication required"));
        }

        var tour = await tourRepository.GetByIdAsync(request.TourId, ct);

        if (tour is null)
        {
            logger.LogWarning("Tour {TourId} not found", request.TourId);

            return Result.Failure<Guid>(
                Error.NotFound("Tour.NotFound", "Tour not found"));
        }

        if (!request.Currency.Equals(tour.Currency, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Currency mismatch for TourPackage. TourCurrency={TourCurrency}, RequestCurrency={RequestCurrency}",
                tour.Currency,
                request.Currency);

            return Result.Failure<Guid>(
                Error.Validation("TourPackage.CurrencyMismatch", "Currency must match the tour currency"));
        }

        if (tour.IsDeleted || tour.Status != TourStatus.Approved)
        {
            logger.LogWarning(
                "Invalid tour state for Tour {TourId}. Status={Status}, IsDeleted={IsDeleted}",
                tour.Id,
                tour.Status,
                tour.IsDeleted);

            return Result.Failure<Guid>(
                Error.Validation("Tour.Invalid", "Tour must be approved and not deleted"));
        }

        if (!currentUser.IsInRole("Admin") &&
            currentUser.UserId != tour.CreatedByUserId)
        {
            logger.LogWarning(
                "User {UserId} not allowed to create package for Tour {TourId}",
                currentUser.UserId,
                tour.Id);

            return Result.Failure<Guid>(
                Error.Forbidden("Not allowed to create package for this tour"));
        }

        TourPackageEntity package;

        try
        {
            package = TourPackageEntity.Create(
                request.TourId,
                request.Name,
                request.Description,
                request.Price,
                request.Currency,
                request.MaxParticipants,
                request.ValidFrom,
                request.ValidTo);

            await packageRepository.AddAsync(package, ct);

            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Guid>(
                Error.Validation("TourPackage.Invalid", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<Guid>(
                Error.Validation("TourPackage.Invalid", ex.Message));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<Guid>(
                new Error("TourPackage.ConcurrencyConflict", "Concurrency conflict"),
                Outcome.Conflict);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex,
                "Database error while creating TourPackage for Tour {TourId}",
                request.TourId);

            return Result.Failure<Guid>(
                Error.Failure("TourPackage.DatabaseError", "Database error occurred"));
        }

        logger.LogInformation(
            "TourPackage {PackageId} created successfully",
            package.Id);

        return Result.Success(package.Id);
    }
}
