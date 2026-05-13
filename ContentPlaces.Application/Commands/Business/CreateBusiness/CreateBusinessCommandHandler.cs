using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;
using BusinessEntity = ContentPlaces.Domain.Entities.Business;

namespace ContentPlaces.Application.Commands.Business.CreateBusiness;

public sealed class CreateBusinessCommandHandler(
    IBusinessRepository businessRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<CreateBusinessCommandHandler> logger)
    : ICommandHandler<CreateBusinessCommand, CreateBusinessResult>
{
    public async Task<Result<CreateBusinessResult>> Handle(
        CreateBusinessCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            {
                return Result<CreateBusinessResult>.Failure(
                    Error.Unauthorized("Authentication is required."),
                    Outcome.Unauthorized);
            }

            if (!await businessRepository.PlaceExistsAsync(request.PlaceId, cancellationToken))
            {
                return Result<CreateBusinessResult>.Failure(
                    new Error("Place.NotFound", $"Place '{request.PlaceId}' was not found or has been deleted."),
                    Outcome.NotFound);
            }

            var slug = string.IsNullOrWhiteSpace(request.Slug)
                ? BusinessEntity.GenerateSlug(request.Name)
                : request.Slug.Trim().ToLowerInvariant();

            if (await businessRepository.AnyAsync(b => b.Slug == slug, cancellationToken))
            {
                return Result<CreateBusinessResult>.Conflict(
                    new Error("Business.SlugConflict", $"A business with slug '{slug}' already exists."));
            }

            var business = BusinessEntity.Create(
                name: request.Name,
                slug: slug,
                businessType: request.BusinessType,
                ownerId: currentUser.UserId.Value,
                location: new Location(request.Latitude, request.Longitude),
                placeId: request.PlaceId,
                description: request.Description,
                address: request.Address,
                city: request.City,
                country: request.Country,
                postalCode: request.PostalCode,
                phone: request.Phone,
                email: request.Email,
                website: request.Website,
                licenseNumber: request.LicenseNumber,
                taxId: request.TaxId);

            await businessRepository.AddAsync(business, cancellationToken);
            await businessRepository.AddPlaceBusinessJunctionAsync(request.PlaceId, business.Id, cancellationToken);

            var saveResult = await SaveAsync(business.Slug, cancellationToken);
            if (saveResult is not null)
            {
                return saveResult;
            }

            // Evict all cached business lists for this place
            await cache.RemoveByTagAsync(ContentPlacesCacheKeys.TagBusinesses, cancellationToken);

            return Result<CreateBusinessResult>.Created(
                new CreateBusinessResult(business.Id, business.Name, business.Slug));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<CreateBusinessResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private async Task<Result<CreateBusinessResult>?> SaveAsync(string slug, CancellationToken ct)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            return null;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict creating business with slug '{Slug}'.", slug);
            return Result<CreateBusinessResult>.Conflict(
                new Error("Business.ConcurrencyConflict", "The record was modified by another request. Please retry."));
        }
    }
}
