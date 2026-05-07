using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.DeleteProfile;

public sealed class DeleteProfileCommandHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache)
    : ICommandHandler<DeleteProfileCommand>
{
    public async Task<Result> Handle(DeleteProfileCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var userId = currentUser.UserId.Value;

        var profile = await profileRepository.FirstOrDefaultAsync(
            filter: p => p.UserId == userId,
            asNoTracking: false,
            ct: ct);

        if (profile is null)
        {
            return Result.Failure(
                ProfileErrors.NotFound,
                Outcome.NotFound);
        }

        // Soft delete — sets IsDeleted = true via AuditableEntity.SoftDelete().
        // EF change tracker detects the mutation and issues UPDATE, not DELETE.
        profile.SoftDelete();

        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(AccountsCacheKeys.UserProfileTag(userId), ct);

        return Result.Success();
    }
}
