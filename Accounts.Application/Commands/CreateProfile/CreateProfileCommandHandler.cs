using Accounts.Application.Abstractions;
using Accounts.Domain.Entities;
using Accounts.Domain.Interfaces;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.CreateProfile;

public sealed class CreateProfileCommandHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    ISecurityUserExistenceChecker securityUserExistenceChecker)
    : ICommandHandler<CreateProfileCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProfileCommand request, CancellationToken cancellationToken)
    {
        var securityUserExists = await securityUserExistenceChecker.ExistsAsync(request.UserId, cancellationToken);
        if (!securityUserExists)
        {
            return Result.Failure<Guid>(Error.NotFound("Security.User", "Security user does not exist"));
        }

        var profileAlreadyExists = await profileRepository.AnyAsync(
            p => p.UserId == request.UserId,
            cancellationToken);

        if (profileAlreadyExists)
        {
            return Result.Failure<Guid>(Error.Conflict("Profile.UserId", "Profile already exists for this user"));
        }

        var profile = Profile.Create(request.UserId, request.FirstName, request.LastName);
        profile.SetDisplayName(request.DisplayName);
        profile.SetAvatarUrl(request.AvatarUrl);

        await profileRepository.AddAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(profile.Id);
    }
}
