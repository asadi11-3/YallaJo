using Security.Application.Interfaces;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser)
    : ICommandHandler<ChangePasswordCommand, ChangePasswordResult>
{
    public async Task<Result<ChangePasswordResult>> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result<ChangePasswordResult>.Failure(
                Error.Unauthorized("User is not authenticated."),
                Outcome.Unauthorized);
        }

        var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken, asNoTracking: false);
        if (user is null) {
            return Result<ChangePasswordResult>.Failure(
               Error.NotFound("User", "User not found."),
               Outcome.NotFound);
        }

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash)) {
            return Result<ChangePasswordResult>.Failure(
                Error.Unauthorized("Current password is incorrect."),
                Outcome.Unauthorized);
        }

        user.SetPasswordHash(passwordHasher.Hash(request.NewPassword));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ChangePasswordResult>.Success(new ChangePasswordResult(true));
    }
}
