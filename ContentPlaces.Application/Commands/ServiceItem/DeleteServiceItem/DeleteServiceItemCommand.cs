using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.ServiceItem.DeleteServiceItem;

public sealed record DeleteServiceItemCommand(Guid Id, Guid BusinessId) : ICommand;
