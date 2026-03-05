using Accounts.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.DeleteProfile;

public sealed class DeleteProfileCommandHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<DeleteProfileCommand>
{
    public async Task<Result> Handle(DeleteProfileCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var profile = await profileRepository.GetByUserIdAsync(currentUser.UserId.Value, ct);
        if (profile is null)
            return Result.Failure(
                Error.NotFound("Profile", "Profile not found."),
                Outcome.NotFound);

        profileRepository.Remove(profile);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
