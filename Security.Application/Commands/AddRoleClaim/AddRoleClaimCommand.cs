using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.AddRoleClaim;

public sealed record AddRoleClaimCommand(Guid RoleId, string ClaimType, string ClaimValue) : ICommand;
