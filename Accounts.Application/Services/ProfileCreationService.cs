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
    public async Task<Result<Guid>> CreateForUserAsync(
        ProfileCreationRequest request,
        CancellationToken cancellationToken = default)
    {
        return await CreateProfileAsync(
            request.UserId,
            request.FirstName,
            request.LastName,
            displayName: null,
            avatarUrl: null,
            cancellationToken);
    }

    public async Task<Result<Guid>> CreateForInvitedUserAsync(
        InvitedProfileCreationRequest request,
        CancellationToken cancellationToken = default)
    {
        return await CreateProfileAsync(
            request.UserId,
            request.FirstName,
            request.LastName,
            request.DisplayName,
            request.AvatarUrl,
            cancellationToken);
    }

    private async Task<Result<Guid>> CreateProfileAsync(
        Guid userId,
        string firstName,
        string lastName,
        string? displayName,
        string? avatarUrl,
        CancellationToken cancellationToken)
    {
        var exists = await profileRepository.AnyAsync(
            p => p.UserId == userId,
            cancellationToken);

        if (exists)
        {
            return Result<Guid>.Conflict(
                Error.Conflict("Profile.UserId", "Profile already exists for this user."));
        }

        var profile = Profile.Create(userId, firstName, lastName);
        profile.SetDisplayName(displayName);
        profile.SetAvatarUrl(avatarUrl);

        await profileRepository.AddAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Created(profile.Id);
    }
}
