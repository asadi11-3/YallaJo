using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.ServiceItem.UpdateServiceItem;

public sealed record UpdateServiceItemCommand(
    Guid Id,
    Guid BusinessId,
    string Name,
    decimal Price,
    int DurationMinutes,
    int MaxCapacity,
    string Currency,
    int SortOrder)
    : ICommand;
