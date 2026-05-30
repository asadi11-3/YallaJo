using ContentPlaces.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.ServiceItem.CreateServiceItem;

public sealed record CreateServiceItemCommand(
    Guid BusinessId,
    string Name,
    decimal Price,
    int DurationMinutes,
    int MaxCapacity,
    string Currency,
    ServiceCategory Category,
    string? Description = null,
    int SortOrder = 0)
    : ICommand<CreateServiceItemResult>;
