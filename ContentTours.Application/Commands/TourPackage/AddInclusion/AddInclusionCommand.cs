using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourPackage.AddInclusion;

public sealed record AddInclusionCommand(
    Guid PackageId,
    string Description,
    int SortOrder
) : ICommand;
