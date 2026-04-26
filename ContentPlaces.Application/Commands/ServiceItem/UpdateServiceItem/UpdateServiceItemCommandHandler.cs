using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.ServiceItem.UpdateServiceItem;

public sealed class UpdateServiceItemCommandHandler(
    IServiceItemRepository serviceItemRepository,
    IBusinessRepository businessRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<UpdateServiceItemCommandHandler> logger)
    : ICommandHandler<UpdateServiceItemCommand>
{
    public async Task<Result> Handle(
        UpdateServiceItemCommand request,
        CancellationToken cancellationToken)
    {
        // ── Auth guard ────────────────────────────────────────────────────────
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Failure(
                Error.Unauthorized("Authentication required."), Outcome.Unauthorized);

        var item = await serviceItemRepository.GetByIdAsync(
            request.Id, cancellationToken, asNoTracking: false);

        // BusinessId in the command is validated when provided (business-scoped route).
        if (item is null || (request.BusinessId != Guid.Empty && item.BusinessId != request.BusinessId))
            return Result.Failure(
                new Error("ServiceItem.NotFound", "Service item not found."), Outcome.NotFound);

        // ── IDOR: caller must own the business (or be admin) ──────────────────
        var business = await businessRepository.GetByIdAsync(item.BusinessId, cancellationToken);
        if (business is null)
            return Result.Failure(
                new Error("Business.NotFound", "Business not found."), Outcome.NotFound);

        var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
            >= RolePrivilegeLevel.Admin;
        if (!isAdminTier && business.OwnerId != currentUser.UserId.Value)
            return Result.Failure(
                Error.Forbidden("You do not own this business."), Outcome.Forbidden);

        // ── Duplicate name check ──────────────────────────────────────────────
        var duplicateName = await serviceItemRepository.AnyAsync(
            s => s.BusinessId == item.BusinessId &&
                 string.Equals(s.Name, request.Name, StringComparison.OrdinalIgnoreCase) &&
                 s.Id != request.Id,
            cancellationToken);

        if (duplicateName)
            return Result.Failure(
                new Error("ServiceItem.Duplicate",
                    $"A service with the name '{request.Name}' already exists."),
                Outcome.Conflict);

        item.Update(
            name:            request.Name,
            price:           request.Price,
            currency:        request.Currency,
            category:        request.Category,
            durationMinutes: request.DurationMinutes,
            maxCapacity:     request.MaxCapacity,
            description:     request.Description,
            sortOrder:       request.SortOrder);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("ServiceItem.ConcurrencyConflict",
                    "A concurrency conflict occurred. Please refresh and try again."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync($"service:{request.Id}", cancellationToken);
        await cache.RemoveByTagAsync($"biz:{item.BusinessId}:services", cancellationToken);

        logger.LogInformation("ServiceItem {ServiceItemId} updated", request.Id);

        return Result.Success();
    }
}
