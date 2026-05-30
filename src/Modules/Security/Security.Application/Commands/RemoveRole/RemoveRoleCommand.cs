using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.RemoveRole;

public sealed record RemoveRoleCommand(Guid UserId, Guid RoleId) : ICommand;
