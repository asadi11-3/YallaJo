using ServiceItemEntity = ContentPlaces.Domain.Entities.ServiceItem;

namespace ContentPlaces.Application.Queries.ServiceItem.Common;

public sealed record ServiceItemDto(
    Guid Id,
    string Name,
    decimal Price,
    int DurationMinutes,
    string Currency)
{
    public static ServiceItemDto From(ServiceItemEntity item) => new(
        item.Id, item.Name, item.Price, item.DurationMinutes, item.Currency);
}
