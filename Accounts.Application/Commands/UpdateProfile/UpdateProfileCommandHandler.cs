using Accounts.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<UpdateProfileCommand, UpdateProfileResult>
{
    public async Task<Result<UpdateProfileResult>> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null) {
            return Result<UpdateProfileResult>.Failure(
                    Error.Unauthorized("Authentication is required."),
                    Outcome.Unauthorized);
        }

        var profile = await profileRepository.GetByUserIdAsync(currentUser.UserId.Value, cancellationToken);
        if (profile is null) {
            return Result<UpdateProfileResult>.Failure(
                   Error.NotFound("Profile", "Profile not found."),
                   Outcome.NotFound);
        }

        profile.UpdateProfile(
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Gender,
            request.Country,
            request.City,
            request.AddressLine);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UpdateProfileResult>.Success(new UpdateProfileResult(
            FirstName: profile.FirstName,
            LastName: profile.LastName));
    }
}
