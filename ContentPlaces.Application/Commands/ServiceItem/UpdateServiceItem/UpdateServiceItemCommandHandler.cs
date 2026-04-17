using System;
using System.Globalization;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Exceptions;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.ServiceItem.UpdateServiceItem;

public sealed class UpdateServiceItemCommandHandler(
    IServiceItemRepository serviceItemRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ILogger<UpdateServiceItemCommandHandler> logger)
    : ICommandHandler<UpdateServiceItemCommand>
{
    public async Task<Result> Handle(
        UpdateServiceItemCommand request,
        CancellationToken cancellationToken)
    {
        var item = await serviceItemRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);

        if (item is null || item.BusinessId != request.BusinessId)
        {
            return Result.Failure(
                new Error("ServiceItem.NotFound", "Service item not found"),
                Outcome.NotFound);
        }

        var duplicateName = await serviceItemRepository.AnyAsync(
            s => s.BusinessId == item.BusinessId &&
                 string.Equals(s.Name, request.Name, StringComparison.OrdinalIgnoreCase) &&
                 s.Id != request.Id,
            cancellationToken);

        if (duplicateName)
        {
            return Result.Failure(
                new Error("ServiceItem.Duplicate", $"A service with the name '{request.Name}' already exists."),
                Outcome.Conflict);
        }

        item.Update(request.Name, request.Price, request.DurationMinutes,
            request.MaxCapacity, request.Currency, request.SortOrder);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ContentPlaceConcurrencyException)
        {
            return Result.Failure(
                new Error(
                    "ServiceItem.ConcurrencyConflict",
                    "A concurrency conflict occurred. Please refresh and try again."),
                Outcome.Conflict);
        }

        logger.LogInformation("ServiceItem {ServiceItemId} updated", request.Id);

        return Result.Success();
    }
}
