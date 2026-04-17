using ContentPlaces.Application.Interfaces;
using ContentPlaces.Contracts.IntegrationEvents;
using ContentPlaces.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
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
            s => s.BusinessId == request.BusinessId &&
                 s.Name.ToLower() == request.Name.ToLower(),
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
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new ServiceItemCreateIntegrationEvent(item.Id, item.BusinessId, item.Name, item.Price, item.Currency),
            cancellationToken);

        logger.LogInformation("ServiceItem {ServiceItemId} created for Business {BusinessId}", item.Id, request.BusinessId);

        return Result<CreateServiceItemResult>.Created(new CreateServiceItemResult(item.Id, item.Name));
    }
}
