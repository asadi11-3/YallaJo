using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.BusinessStaff.RemoveBusinessStaff;

public sealed record RemoveBusinessStaffCommand(Guid Id)
    : ICommand;
