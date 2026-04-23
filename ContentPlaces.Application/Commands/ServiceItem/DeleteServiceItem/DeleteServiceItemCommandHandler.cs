using ContentPlaces.Application.Interfaces;
using ContentPlaces.Contracts.IntegrationEvents;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.ServiceItem.DeleteServiceItem;

public sealed class DeleteServiceItemCommandHandler(
    IServiceItemRepository serviceItemRepository,
    IBusinessRepository businessRepository,
    IContentPlacesUnitOfWork unitOfWork,
    IContentPlacesOutboxWriter outbox,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<DeleteServiceItemCommandHandler> logger)
    : ICommandHandler<DeleteServiceItemCommand>
{
    public async Task<Result> Handle(
        DeleteServiceItemCommand request,
        CancellationToken cancellationToken)
    {
        // ── Auth guard ────────────────────────────────────────────────────────
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Failure(
                Error.Unauthorized("Authentication required."), Outcome.Unauthorized);

        var item = await serviceItemRepository.GetByIdAsync(
            request.Id, cancellationToken, asNoTracking: false);

        // BusinessId in the command is Guid.Empty when called from item-level route (no businessId in URL).
        if (item is null || (request.BusinessId != Guid.Empty && item.BusinessId != request.BusinessId))
            return Result.Failure(
                new Error("ServiceItem.NotFound", "Service item not found."), Outcome.NotFound);

        // ── IDOR: caller must own the business (or be admin) ──────────────────
        var business = await businessRepository.GetByIdAsync(item.BusinessId, cancellationToken);
        if (business is null)
            return Result.Failure(
                new Error("Business.NotFound", "Business not found."), Outcome.NotFound);

        var isAdmin = currentUser.IsInRole("Admin");
        if (!isAdmin && business.OwnerId != currentUser.UserId.Value)
            return Result.Failure(
                Error.Forbidden("You do not own this business."), Outcome.Forbidden);

        item.SoftDelete();

        // ── Enqueue outbox BEFORE save — commits atomically with soft-delete ───
        outbox.Enqueue(new ServiceItemDeletedIntegrationEvent(item.Id, item.BusinessId));

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

        logger.LogInformation("ServiceItem {ServiceItemId} soft-deleted", request.Id);

        return Result.Success();
    }
}
