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
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<ChangePasswordResult>.Failure(
                Error.Unauthorized("Authentication is required."),
                Outcome.Unauthorized);

        var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, ct, asNoTracking: false);
        if (user is null)
            return Result<ChangePasswordResult>.Failure(
                Error.NotFound("User", "User not found."),
                Outcome.NotFound);

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result<ChangePasswordResult>.Failure(
                Error.Unauthorized("Current password is incorrect."),
                Outcome.Unauthorized);

        user.SetPasswordHash(passwordHasher.Hash(request.NewPassword));
        await unitOfWork.SaveChangesAsync(ct);

        return Result<ChangePasswordResult>.Success(new ChangePasswordResult(true));
    }
}
