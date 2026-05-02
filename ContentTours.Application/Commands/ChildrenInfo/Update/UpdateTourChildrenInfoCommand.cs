using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.ChildrenInfo.Update;

public sealed record UpdateTourChildrenInfoCommand(
    Guid TourId,
    bool AllowsChildren,
    int? AgeRestriction,
    int? MinChildAge,
    int? MaxChildAge,
    string? ChildFacilities) : ICommand;
