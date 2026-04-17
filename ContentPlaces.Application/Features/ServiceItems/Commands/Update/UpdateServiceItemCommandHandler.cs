using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Features.ServiceItems.Commands.Update
{
    public sealed class UpdateServiceItemCommandHandler : IRequestHandler<UpdateServiceItemCommand, Guid>
    {
        private readonly IServiceItemRepository _serviceItemRepository;
        private readonly IContentPlacesUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateServiceItemCommandHandler> _logger;

        public UpdateServiceItemCommandHandler(
            IServiceItemRepository serviceItemRepository,
            IContentPlacesUnitOfWork unitOfWork,
            ILogger<UpdateServiceItemCommandHandler> logger)
        {
            _serviceItemRepository = serviceItemRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Guid> Handle(UpdateServiceItemCommand request, CancellationToken ct)
        {
            _logger.LogInformation("Updating Service Item {ServiceItemId}", request.Id);

            var serviceItem = await _serviceItemRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);

            if (serviceItem == null)
            {
                _logger.LogWarning("Service Item with ID {ServiceItemId} not found", request.Id);
                throw new Exception("ServiceItem.NotFound");
            }

            bool serviceExists = await _serviceItemRepository.AnyAsync(
                s => s.BusinessId == serviceItem.BusinessId
                  && s.Name.ToLower() == request.Name.ToLower()
                  && s.Id != request.Id,
                ct);

            if (serviceExists)
            {
                _logger.LogWarning("Service Item name '{ServiceName}' already exists for Business {BusinessId}", request.Name, serviceItem.BusinessId);
                throw new InvalidOperationException($"A service with the name '{request.Name}' already exists for this business.");
            }

            /*
            var business = await _placeRepository.GetByIdAsync(serviceItem.BusinessId, ct);
            bool isOwner = business.OwnerId == _currentUser.Id;
            bool isAdmin = _currentUser.IsInRole("Admin") || _currentUser.IsInRole("SuperAdmin");

            if (!isOwner && !isAdmin)
            {
                throw new UnauthorizedAccessException("You do not have permission to update this service.");
            }
            */

            serviceItem.Update(
                request.Name,
                request.Price,
                request.DurationMinutes,
                request.MaxCapacity,
                request.Currency,
                request.SortOrder);

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Service Item {ServiceItemId} updated successfully", request.Id);

            return request.Id;
        }
    }
}
