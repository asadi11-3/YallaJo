using Security.Domain.Entities;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.AddUserClaim;

public sealed class AddUserClaimCommandHandler(
    IUserRepository userRepository,
    IUserClaimRepository userClaimRepository,
    ISecurityUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<AddUserClaimCommand>
{
    public async Task<Result> Handle(AddUserClaimCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return Result.Failure(UserErrors.NotFound, Outcome.NotFound);

        if (await userClaimRepository.ExistsAsync(request.UserId, request.ClaimType, request.ClaimValue, ct))
            return Result.Failure(
                new Error("UserClaim.Duplicate", "This claim already exists on the user."),
                Outcome.Conflict);

        var claim = UserClaim.Create(request.UserId, request.ClaimType, request.ClaimValue);
        await userClaimRepository.AddAsync(claim, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
