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
        CancellationToken cancellationToken)
    {
        var profile = await profileRepository.GetByUserIdAsync(currentUser.UserId.Value, cancellationToken);
        if (profile is null)
        {
            return Result<DeleteAvatarResult>.Failure(
               Error.NotFound("Profile", "Profile not found."),
               Outcome.NotFound);
        }

        profile.DeleteAvatar();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DeleteAvatarResult>.Success(new DeleteAvatarResult(true));
    }
}
