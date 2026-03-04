using Accounts.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.DeleteAvatar;

public sealed class DeleteAvatarCommandHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<DeleteAvatarCommand, DeleteAvatarResult>
{
    public async Task<Result<DeleteAvatarResult>> Handle(
        DeleteAvatarCommand request,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<DeleteAvatarResult>.Failure(
                Error.Unauthorized("Authentication is required."),
                Outcome.Unauthorized);

        var profile = await profileRepository.GetByUserIdAsync(currentUser.UserId.Value, ct);
        if (profile is null)
            return Result<DeleteAvatarResult>.Failure(
                Error.NotFound("Profile", "Profile not found."),
                Outcome.NotFound);

        profile.DeleteAvatar();
        await unitOfWork.SaveChangesAsync(ct);

        return Result<DeleteAvatarResult>.Success(new DeleteAvatarResult(true));
    }
}
