using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.RemoveUserClaim;

public sealed class RemoveUserClaimCommandHandler(
    IUserRepository userRepository,
    IUserClaimRepository userClaimRepository,
    ISecurityUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<RemoveUserClaimCommand>
{
    public async Task<Result> Handle(RemoveUserClaimCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return Result.Failure(UserErrors.NotFound, Outcome.NotFound);

        var claim = await userClaimRepository.GetByIdAsync(request.ClaimId, ct);
        if (claim is null || claim.UserId != request.UserId) {
            return Result.Failure(
                new Error("NotFound.UserClaim", "The specified claim was not found on this user."),
                Outcome.NotFound);
        }

        userClaimRepository.Remove(claim);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
