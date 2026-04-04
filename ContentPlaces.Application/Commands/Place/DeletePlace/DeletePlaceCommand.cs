using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.Place.DeletePlace;

public sealed record DeletePlaceCommand(Guid PlaceId) : ICommand;
