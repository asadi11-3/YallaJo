using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.CreateRole;

public sealed record CreateRoleCommand(string Name, string? Description) : ICommand<CreateRoleResult>;
