using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourPackage.CreateTourPackage;

public sealed record CreateTourPackageCommand(
    Guid TourId,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int? MaxParticipants,
    DateTime? ValidFrom,
    DateTime? ValidTo
) : ICommand<Guid>;
