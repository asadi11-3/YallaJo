using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.Place.FeaturePlace;

public sealed record FeaturePlaceCommand(Guid PlaceId, bool IsFeatured) : ICommand;
