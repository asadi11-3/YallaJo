using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourPackage.CreateTourPackage;

public sealed record CreateTourPackageCommand(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int? MaxParticipants,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    IReadOnlyCollection<Guid> IncludedTourIds,
    IReadOnlyCollection<string> Inclusions
) : ICommand<Guid>;
