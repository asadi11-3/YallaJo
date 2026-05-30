using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.UpdatePhone;

public sealed record UpdatePrimaryPhoneCommand(
    string PhoneNumber) : ICommand<UpdatePrimaryPhoneResult>;
