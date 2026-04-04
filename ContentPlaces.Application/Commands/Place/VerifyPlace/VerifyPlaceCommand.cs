using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.Place.VerifyPlace;

public sealed record VerifyPlaceCommand(Guid PlaceId, bool IsVerified) : ICommand;
