using ContentPlaces.Application.Features.ServiceItems.Events;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ContentPlaces.Application.Features.ServiceItems.Commands.Create
{
    public sealed class CreateServiceItemCommandHandler : IRequestHandler<CreateServiceItemCommand, Guid>
    {
        private readonly IServiceItemRepository _serviceItemRepository;
        private readonly IContentPlacesUnitOfWork _unitOfWork;
        private readonly ILogger<CreateServiceItemCommandHandler> _logger;
        private readonly IPublisher _publisher;

        public CreateServiceItemCommandHandler(
            IServiceItemRepository serviceItemRepository,
            IContentPlacesUnitOfWork unitOfWork,
            ILogger<CreateServiceItemCommandHandler> logger,
            IPublisher publisher)
        {
            _serviceItemRepository = serviceItemRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _publisher = publisher;
        }

        public async Task<Guid> Handle(CreateServiceItemCommand request, CancellationToken ct)
        {
            _logger.LogInformation("Creating new Service Item for Business {BusinessId}", request.BusinessId);

            bool serviceExists = await _serviceItemRepository.AnyAsync(
                s => s.BusinessId == request.BusinessId && s.Name.ToLower() == request.Name.ToLower(),
                ct);

            if (serviceExists)
            {
                _logger.LogWarning("Service Item '{ServiceName}' already exists for Business {BusinessId}", request.Name, request.BusinessId);

                throw new InvalidOperationException($"A service with the name '{request.Name}' already exists for this business.");
            }

            var newServiceItem = ServiceItem.Create(
                request.BusinessId,
                request.Name,
                request.Price,
                request.DurationMinutes,
                request.MaxCapacity,
                request.Currency,
                request.SortOrder);

            await _serviceItemRepository.AddAsync(newServiceItem, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var integrationEvent = new ServiceItemCreateIntegrationEvent(
                newServiceItem.Id,
                newServiceItem.BusinessId,
                newServiceItem.Name,
                newServiceItem.Price,
                newServiceItem.Currency
            );

            await _publisher.Publish(integrationEvent, ct);

            _logger.LogInformation("Service Item {ServiceItemId} created & event published", newServiceItem.Id);

            return newServiceItem.Id;
        }
    }
}
