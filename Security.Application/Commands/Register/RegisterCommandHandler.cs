using Security.Application.Interfaces;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.Register;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService)
    : ICommandHandler<RegisterCommand, RegisterResult>
{
    public async Task<Result<RegisterResult>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await userRepository.AnyAsync(
            u => u.Emails.Any(e => e.Address == normalizedEmail),
            cancellationToken);

        if (emailExists)
        {
            return Result<RegisterResult>.Conflict(
                Error.Conflict("User.Email", "An account with this email already exists."));
        }

        var user = User.Register(normalizedEmail);

        var passwordHash = passwordHasher.Hash(request.Password);
        user.SetPasswordHash(passwordHash);

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var tokenData = new UserTokenData(
            UserId: user.Id,
            Email: normalizedEmail,
            Roles: [],
            AdditionalClaims: []);

        var accessToken = jwtTokenService.GenerateAccessToken(tokenData);

        return Result<RegisterResult>.Created(new RegisterResult(user.Id, accessToken));
    }
}
