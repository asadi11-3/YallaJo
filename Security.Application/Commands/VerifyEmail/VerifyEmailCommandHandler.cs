using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler(
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork)
    : ICommandHandler<VerifyEmailCommand>
{
    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithEmailsAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("User", "User not found"));
        }

        var email = user.VerifyEmail(request.EmailId);
        if (email is null)
        {
            return Result.Failure(Error.NotFound("Email", "Email not found for this user"));
        }

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
