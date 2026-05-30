using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.RemoveUserClaim;

public sealed record RemoveUserClaimCommand(Guid UserId, Guid ClaimId) : ICommand;
