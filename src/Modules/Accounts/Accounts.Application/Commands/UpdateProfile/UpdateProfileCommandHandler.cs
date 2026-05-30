using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache)
    : ICommandHandler<UpdateProfileCommand, UpdateProfileResult>
{
    public async Task<Result<UpdateProfileResult>> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<UpdateProfileResult>.Failure(
                Error.Unauthorized("Authentication is required."),
                Outcome.Unauthorized);
        }

        var userId = currentUser.UserId.Value;

        var profile = await profileRepository.FirstOrDefaultAsync(
            filter: p => p.UserId == userId,
            asNoTracking: false,
            ct: cancellationToken);

        if (profile is null)
        {
            return Result<UpdateProfileResult>.Failure(
                ProfileErrors.NotFound,
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
        await cache.RemoveByTagAsync(AccountsCacheKeys.UserProfileTag(userId), cancellationToken);

        return Result<UpdateProfileResult>.Success(new UpdateProfileResult(
            FirstName: profile.FirstName,
            LastName:  profile.LastName));
    }
}
