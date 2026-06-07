using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Contracts.IntegrationEvents;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using ServiceItemEntity = ContentPlaces.Domain.Entities.ServiceItem;

namespace ContentPlaces.Application.Commands.ServiceItem.CreateServiceItem;

public sealed class CreateServiceItemCommandHandler(
    IServiceItemRepository serviceItemRepository,
    IBusinessRepository businessRepository,
    IContentPlacesUnitOfWork unitOfWork,
    IContentPlacesOutboxWriter outbox,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<CreateServiceItemCommandHandler> logger)
    : ICommandHandler<CreateServiceItemCommand, CreateServiceItemResult>
{
    public async Task<Result<CreateServiceItemResult>> Handle(
        CreateServiceItemCommand request,
        CancellationToken cancellationToken)
    {
        // ── IDOR: caller must own the business ────────────────────────────────
        if (currentUser.UserId is null)
            return Result<CreateServiceItemResult>.Failure(
                Error.Unauthorized("Authentication required."), Outcome.Unauthorized);

        var business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
        if (business is null)
            return Result<CreateServiceItemResult>.Failure(
                new Error("Business.NotFound", "Business not found."), Outcome.NotFound);

        var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles) >= RolePrivilegeLevel.Admin;

        if (!isAdminTier && business.OwnerId != currentUser.UserId.Value)
            return Result<CreateServiceItemResult>.Failure(
                Error.Forbidden("You do not own this business."), Outcome.Forbidden);

        // ── Duplicate name check ──────────────────────────────────────────────
        var exists = await serviceItemRepository.AnyAsync(
            s => string.Equals(s.Name, request.Name, StringComparison.OrdinalIgnoreCase)
                 && s.BusinessId == request.BusinessId,
            cancellationToken);

        if (exists)
            return Result<CreateServiceItemResult>.Failure(
                new Error("ServiceItem.Duplicate",
                    $"A service with the name '{request.Name}' already exists for this business."),
                Outcome.Conflict);

        // ── Create entity ─────────────────────────────────────────────────────
        var item = ServiceItemEntity.Create(
            businessId:      request.BusinessId,
            name:            request.Name,
            price:           request.Price,
            currency:        request.Currency,
            category:        request.Category,
            durationMinutes: request.DurationMinutes,
            maxCapacity:     request.MaxCapacity,
            description:     request.Description,
            sortOrder:       request.SortOrder);

        await serviceItemRepository.AddAsync(item, cancellationToken);

        // ── Enqueue outbox BEFORE save — commits atomically with the item row ──
        outbox.Enqueue(new ServiceItemCreatedIntegrationEvent(
            item.Id, item.BusinessId, item.Name, item.Price, item.Currency));

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<CreateServiceItemResult>.Failure(
                new Error("ServiceItem.ConcurrencyConflict",
                    "A concurrency conflict occurred. Please refresh and try again."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(ContentPlacesCacheKeys.TagForBusinessServices(request.BusinessId), cancellationToken);

        logger.LogInformation(
            "ServiceItem {ServiceItemId} created for Business {BusinessId}",
            item.Id, request.BusinessId);

        return Result<CreateServiceItemResult>.Created(
            new CreateServiceItemResult(item.Id, item.Name));
    }
}
