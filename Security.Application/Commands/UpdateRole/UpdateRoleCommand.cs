using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.UpdateRole;

public sealed record UpdateRoleCommand(Guid RoleId, string? Description) : ICommand;
