using ServiceItemEntity = ContentPlaces.Domain.Entities.ServiceItem;

namespace ContentPlaces.Application.Queries.ServiceItem.Common;

public sealed record ServiceItemDetailDto(
    Guid Id,
    Guid BusinessId,
    string Name,
    decimal Price,
    int DurationMinutes,
    int MaxCapacity,
    string Currency,
    int SortOrder,
    bool IsAvailable)
{
    public static ServiceItemDetailDto From(ServiceItemEntity item) => new(
        item.Id, item.BusinessId, item.Name, item.Price, item.DurationMinutes,
        item.MaxCapacity, item.Currency, item.SortOrder, item.IsAvailable);
}
