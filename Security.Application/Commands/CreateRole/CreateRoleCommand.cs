// Security.Application/Commands/CreateRole/CreateRoleCommand.cs
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.CreateRole;

public sealed record CreateRoleResult(Guid Id, string Name);

public sealed record CreateRoleCommand(string Name, string? Description) : ICommand<CreateRoleResult>;
