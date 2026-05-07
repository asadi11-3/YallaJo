using ContentTours.Application.Queries.Tour.Common;
using ContentTours.Application.Queries.TourPackage.Common;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

using ContentTours.Domain.Repositories;

namespace ContentTours.Application.Queries.TourPackage.GetTourPackageById;

public sealed class GetTourPackageByIdQueryHandler(
    ITourPackageRepository repository,
    ICurrentUser currentUser,
    ILogger<GetTourPackageByIdQueryHandler> logger)
    : IQueryHandler<GetTourPackageByIdQuery, TourPackageDetailDto>
{
    public async Task<Result<TourPackageDetailDto>> Handle(
        GetTourPackageByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var package = await repository
                .GetByIdWithDetailsAsync(request.Id, cancellationToken)
                .ConfigureAwait(false);

            if (package is null)
            {
                logger.LogInformation("TourPackage {Id} not found.", request.Id);
                return Result.Failure<TourPackageDetailDto>(
                    new Error("TourPackage.NotFound", $"Tour package '{request.Id}' was not found."),
                    Outcome.NotFound);
            }

            var includedTours = package.IncludedTours
                .Where(link => link.Tour is not null)
                .Select(link => new TourSummaryDto(
                    Id:            link.Tour.Id,
                    Name:          link.Tour.Name,
                    Slug:          link.Tour.Slug,
                    BasePrice:     link.Tour.BasePrice.Amount,
                    Currency:      link.Tour.Currency,
                    SalePrice:     link.Tour.SalePrice,
                    AverageRating: link.Tour.AverageRating,
                    ReviewCount:   link.Tour.ReviewCount,
                    BookingCount:  link.Tour.BookingCount,
                    IsFeatured:    link.Tour.IsFeatured,
                    Status:        link.Tour.Status.ToString(),
                    CreatedAt:     link.Tour.CreatedAt))
                .ToList();

            var inclusions = package.Inclusions
                .OrderBy(i => i.SortOrder)
                .Select(i => new TourPackageInclusionDto(i.Id, i.Description, i.SortOrder))
                .ToList();

            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;

            var dto = new TourPackageDetailDto(
                Id:               package.Id,
                CreatedByUserId:  package.CreatedByUserId,
                Name:             package.Name,
                Description:      package.Description,
                PriceAmount:      package.Price.Amount,
                Currency:         package.Currency,
                MaxParticipants:  package.MaxParticipants,
                ValidFrom:        package.ValidFrom,
                ValidTo:          package.ValidTo,
                IsActive:         package.IsActive,
                CreatedAt:        package.CreatedAt,
                RowVersion:       Convert.ToBase64String(package.RowVersion ?? Array.Empty<byte>()),
                IncludedTours:    includedTours,
                Inclusions:       inclusions,
                IsDeleted:        isAdminTier ? package.IsDeleted : null);

            return Result.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<TourPackageDetailDto>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
