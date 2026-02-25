using Security.Application.Helpers;
using Security.Application.Interfaces;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.Register;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher)
    : ICommandHandler<RegisterCommand, RegisterResult>
{
    public async Task<Result<RegisterResult>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = SecurityGuard.NormalizeEmail(request.Email);

        var emailExists = await userRepository.AnyAsync(
            u => u.Emails.Any(e => e.Address == normalizedEmail),
            cancellationToken);

        if (emailExists)
            return Result<RegisterResult>.Conflict(
                Error.Conflict("User.Email", "An account with this email already exists."));

        var user = User.Register(normalizedEmail);

        user.SetPasswordHash(passwordHasher.Hash(request.Password));

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        // SaveChanges dispatches UserCreatedDomainEvent → outbox → Auth module sends OTP email

        return Result<RegisterResult>.Created(new RegisterResult(user.Id));
    }
}
