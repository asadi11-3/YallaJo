using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Agency.ApplyToAgency;

public sealed record ApplyToAgencyCommand(
    Guid AgencyUserId,
    string Message) : ICommand<Guid>;
