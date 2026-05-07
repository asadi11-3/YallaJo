using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourPackage.UpdateTourPackage;

public sealed record UpdateTourPackageCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int? MaxParticipants,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    IReadOnlyCollection<Guid> IncludedTourIds,
    byte[] RowVersion
) : ICommand;
