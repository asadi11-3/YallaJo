using ContentPlaces.Application.Interfaces;
using ContentPlaces.Contracts.IntegrationEvents;
using ContentPlaces.Domain.Exceptions;
using ContentPlaces.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.ServiceItem.DeleteServiceItem;

public sealed class DeleteServiceItemCommandHandler(
    IServiceItemRepository serviceItemRepository,
    IContentPlacesUnitOfWork unitOfWork,
    IPublisher publisher,
    ILogger<DeleteServiceItemCommandHandler> logger)
    : ICommandHandler<DeleteServiceItemCommand>
{
    public async Task<Result> Handle(
        DeleteServiceItemCommand request,
        CancellationToken cancellationToken)
    {
        var item = await serviceItemRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);

        if (item is null || item.BusinessId != request.BusinessId)
        {
            return Result.Failure(
                new Error("ServiceItem.NotFound", "Service item not found"),
                Outcome.NotFound);
        }

        item.SoftDelete();
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

        await publisher.Publish(
            new ServiceItemDeletedIntegrationEvent(item.Id, item.BusinessId),
            cancellationToken);

        logger.LogInformation("ServiceItem {ServiceItemId} soft-deleted", request.Id);

        return Result.Success();
    }
}
