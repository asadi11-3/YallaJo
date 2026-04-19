using ContentPlaces.Application.Queries.ServiceItem.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.ServiceItem.GetServiceItemById;

public sealed class GetServiceItemByIdQueryHandler(
    IServiceItemRepository serviceItemRepository,
    ILogger<GetServiceItemByIdQueryHandler> logger)
    : IQueryHandler<GetServiceItemByIdQuery, ServiceItemDetailDto>
{
    public async Task<Result<ServiceItemDetailDto>> Handle(
        GetServiceItemByIdQuery request,
        CancellationToken cancellationToken)
    {
        var item = await serviceItemRepository.GetByIdAsync(request.ServiceItemId, cancellationToken);

        if (item is null || item.BusinessId != request.BusinessId)
        {
            return Result<ServiceItemDetailDto>.Failure(
                new Error("ServiceItem.NotFound", "Service item not found"),
                Outcome.NotFound);
        }

        logger.LogInformation("Fetched ServiceItem {ServiceItemId}", request.ServiceItemId);

        return Result<ServiceItemDetailDto>.Success(ServiceItemDetailDto.From(item));
    }
}
