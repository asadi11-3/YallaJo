using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.UpdatePhone;

public sealed record UpdatePrimaryPhoneResult(bool Success, string PhoneNumber);

public sealed record UpdatePrimaryPhoneCommand(
    string PhoneNumber) : ICommand<UpdatePrimaryPhoneResult>;
