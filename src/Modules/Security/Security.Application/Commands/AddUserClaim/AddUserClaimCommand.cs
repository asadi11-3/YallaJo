using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.AddUserClaim;

public sealed record AddUserClaimCommand(Guid UserId, string ClaimType, string ClaimValue) : ICommand;
