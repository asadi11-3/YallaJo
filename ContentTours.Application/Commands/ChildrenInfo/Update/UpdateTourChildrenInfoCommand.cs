using MediatR;
using System.Windows.Input;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
namespace ContentTours.Application.Commands.ChildrenInfo.Update;

public sealed record UpdateTourChildrenInfoCommand(
    Guid TourId,
    bool IsChildFriendly,
    int? AgeRestriction,
    int? MinChildAge,
    int? MaxChildAge,
    string? ChildFacilities
) : ICommand<Result>;
