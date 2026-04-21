using ContentPlaces.Application.Interfaces;
using ContentPlaces.Contracts.IntegrationEvents;
using ContentPlaces.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using ServiceItemEntity = ContentPlaces.Domain.Entities.ServiceItem;

namespace ContentPlaces.Application.Commands.ServiceItem.CreateServiceItem;

public sealed class CreateServiceItemCommandHandler(
    IServiceItemRepository serviceItemRepository,
    IContentPlacesUnitOfWork unitOfWork,
    IPublisher publisher,
    ILogger<CreateServiceItemCommandHandler> logger)
    : ICommandHandler<CreateServiceItemCommand, CreateServiceItemResult>
{
    public async Task<Result<CreateServiceItemResult>> Handle(
        CreateServiceItemCommand request,
        CancellationToken cancellationToken)
    {
        var exists = await serviceItemRepository.AnyAsync(
            s => string.Equals(s.Name, request.Name, StringComparison.OrdinalIgnoreCase) && s.BusinessId == request.BusinessId,
            cancellationToken);

        if (exists)
        {
            return Result<CreateServiceItemResult>.Failure(
                new Error("ServiceItem.Duplicate", $"A service with the name '{request.Name}' already exists for this business."),
                Outcome.Conflict);
        }

        var item = ServiceItemEntity.Create(
            request.BusinessId, request.Name, request.Price,
            request.DurationMinutes, request.MaxCapacity,
            request.Currency, request.SortOrder);

        await serviceItemRepository.AddAsync(item, cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<CreateServiceItemResult>.Failure(
                new Error(
                    "ServiceItem.ConcurrencyConflict",
                    "A concurrency conflict occurred. Please refresh and try again."),
                Outcome.Conflict);
        }

        await publisher.Publish(
            new ServiceItemCreateIntegrationEvent(item.Id, item.BusinessId, item.Name, item.Price, item.Currency),
            cancellationToken);

        logger.LogInformation("ServiceItem {ServiceItemId} created for Business {BusinessId}", item.Id, request.BusinessId);

        return Result<CreateServiceItemResult>.Created(new CreateServiceItemResult(item.Id, item.Name));
    }
}
