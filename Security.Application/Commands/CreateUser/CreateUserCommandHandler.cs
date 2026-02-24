using Security.Application.Helpers;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.CreateUser;


public sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork)
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = SecurityGuard.NormalizeEmail(request.Email);

        var emailExists = await userRepository.AnyAsync(
            u => u.Emails.Any(e => e.Address == normalizedEmail),
            cancellationToken);

        if (emailExists)
        {
            return Result<Guid>.Conflict(Error.Conflict("User.Email", "Email is already registered"));
        }

        var user = User.Register(normalizedEmail);

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Created(user.Id);
    }
}