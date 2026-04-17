using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.BusinessAmenity.RemoveBusinessAmenity;

public sealed record RemoveBusinessAmenityCommand(Guid AmenityId) : ICommand;
