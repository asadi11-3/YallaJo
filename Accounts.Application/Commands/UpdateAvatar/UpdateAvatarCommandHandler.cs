using Accounts.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.UpdateAvatar;

public sealed class UpdateAvatarCommandHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<UpdateAvatarCommand, UpdateAvatarResult>
{
    public async Task<Result<UpdateAvatarResult>> Handle(
        UpdateAvatarCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null) {
            return Result<UpdateAvatarResult>.Failure(
                   Error.Unauthorized("Authentication is required."),
                   Outcome.Unauthorized);
        }

        var profile = await profileRepository.GetByUserIdAsync(currentUser.UserId.Value, cancellationToken);
        if (profile is null)
        {
            return Result<UpdateAvatarResult>.Failure(
                Error.NotFound("Profile", "Profile not found."),
                Outcome.NotFound);
        }

        profile.UpdateAvatar(request.AvatarUrl);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UpdateAvatarResult>.Success(new UpdateAvatarResult(profile.AvatarUrl!));
    }
}
