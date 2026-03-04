
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.AssignRole;

public sealed record AssignRoleCommand(Guid UserId, Guid RoleId) : ICommand;
