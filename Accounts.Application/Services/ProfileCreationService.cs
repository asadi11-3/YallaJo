using Accounts.Contracts.Abstractions;
using Accounts.Domain.Entities;
using Accounts.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Services;

/// <summary>
/// Implementation of <see cref="IProfileCreationService"/> — keeps the
/// <c>Profile</c> aggregate inside the Accounts module. Used by Auth's
/// orchestration (InviteUser) to create the profile row alongside the newly
/// invited Security identity. Identity existence is already guaranteed by the
/// caller, so we do NOT re-check it here (that would reintroduce a direct
/// Security dependency path when not needed).
/// </summary>
internal sealed class ProfileCreationService(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork)
    : IProfileCreationService
{
    public async Task<Result<Guid>> CreateForInvitedUserAsync(
        InvitedProfileCreationRequest request,
        CancellationToken cancellationToken = default)
    {
        var exists = await profileRepository.AnyAsync(
            p => p.UserId == request.UserId,
            cancellationToken);

        if (exists)
        {
            return Result<Guid>.Conflict(
                Error.Conflict("Profile.UserId", "Profile already exists for this user."));
        }

        var profile = Profile.Create(request.UserId, request.FirstName, request.LastName);
        profile.SetDisplayName(request.DisplayName);
        profile.SetAvatarUrl(request.AvatarUrl);

        await profileRepository.AddAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Created(profile.Id);
    }
}
