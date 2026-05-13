using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.ChildrenInfo.Update;

// AgeRestriction is intentionally not part of this command.
// It is a general Tour field owned by CreateTour / UpdateTour only.
// ChildrenInfo owns: AllowsChildren, MinChildAge, MaxChildAge, ChildFacilities.
public sealed record UpdateTourChildrenInfoCommand(
    Guid TourId,
    bool AllowsChildren,
    int? MinChildAge,
    int? MaxChildAge,
    string? ChildFacilities) : ICommand;
