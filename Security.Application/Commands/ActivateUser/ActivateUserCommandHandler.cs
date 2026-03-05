using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.ActivateUser;

public sealed class ActivateUserCommandHandler(
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<ActivateUserCommand>
{
    public async Task<Result> Handle(ActivateUserCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Result.Failure(Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var user = await userRepository.GetByIdAsync(request.UserId, ct, asNoTracking: false);
        if (user is null)
            return Result.Failure(UserErrors.NotFound, Outcome.NotFound);

        if (user.IsActive)
            return Result.Success(); // idempotent — already active

        user.Activate();
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
