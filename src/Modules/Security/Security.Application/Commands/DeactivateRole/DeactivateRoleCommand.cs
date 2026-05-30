using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.DeactivateRole;

public sealed record DeactivateRoleCommand(Guid RoleId) : ICommand;
