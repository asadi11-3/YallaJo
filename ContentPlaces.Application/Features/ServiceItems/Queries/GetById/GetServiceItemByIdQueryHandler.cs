using ContentPlaces.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Features.ServiceItems.Queries.GetById;

public sealed class GetServiceItemByIdQueryHandler : IRequestHandler<GetServiceItemByIdQuery, ServiceItemDetailResponse>
{
    private readonly IServiceItemRepository _repository;
    private readonly ILogger<GetServiceItemByIdQueryHandler> _logger;

    public GetServiceItemByIdQueryHandler(
        IServiceItemRepository repository,
        ILogger<GetServiceItemByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ServiceItemDetailResponse> Handle(GetServiceItemByIdQuery request, CancellationToken ct)
    {
        var serviceItem = await _repository.GetByIdAsync(request.ServiceItemId, ct, asNoTracking: true);

        if (serviceItem == null || serviceItem.BusinessId != request.BusinessId)
        {
            _logger.LogWarning("Service Item {ServiceItemId} not found for Business {BusinessId}", request.ServiceItemId, request.BusinessId);
            throw new Exception("ServiceItem.NotFound");
        }

        return new ServiceItemDetailResponse(
            serviceItem.Id,
            serviceItem.Name,
            serviceItem.Price,
            serviceItem.DurationMinutes,
            serviceItem.MaxCapacity,
            serviceItem.Currency,
            serviceItem.SortOrder
        );
    }
}
