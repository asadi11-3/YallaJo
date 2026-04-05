using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Caching;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.ActivateUser;

public sealed class ActivateUserCommandHandler(
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<ActivateUserCommand>
{
    public async Task<Result> Handle(ActivateUserCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct, asNoTracking: false);
        if (user is null)
            return Result.Failure(UserErrors.NotFound, Outcome.NotFound);

        if (user.IsActive)
            return Result.Success(); // idempotent — already active

        user.Activate();
        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UserTag(request.UserId), ct);
        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, ct);

        return Result.Success();
    }
}
