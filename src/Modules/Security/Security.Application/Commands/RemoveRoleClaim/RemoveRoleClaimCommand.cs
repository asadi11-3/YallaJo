using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.RemoveRoleClaim;

public sealed record RemoveRoleClaimCommand(Guid RoleId, Guid ClaimId) : ICommand;
