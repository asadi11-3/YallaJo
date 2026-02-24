using Security.Application.Interfaces;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService)
    : ICommandHandler<LoginCommand, LoginResult>
{
    private static readonly Result<LoginResult> _invalidCredentials =
        Result<LoginResult>.Failure(
            Error.Unauthorized("Invalid email or password."),
            Outcome.Unauthorized);

    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await userRepository.GetByEmailWithDetailsAsync(normalizedEmail, cancellationToken);

        if (user is null)
            return _invalidCredentials;

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return _invalidCredentials;

        // Build role names (deduplicated)
        var roles = user.UserRoles
            .Where(ur => ur.Role.IsActive)
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Merge user claims + role claims, deduplicated by (Type, Value)
        var userClaims = user.UserClaims
            .Select(c => new ClaimEntry(c.ClaimType, c.ClaimValue));

        var roleClaims = user.UserRoles
            .Where(ur => ur.Role.IsActive)
            .SelectMany(ur => ur.Role.RoleClaims)
            .Select(c => new ClaimEntry(c.ClaimType, c.ClaimValue));

        var additionalClaims = userClaims
            .Concat(roleClaims)
            .DistinctBy(c => (c.Type, c.Value))
            .ToList();

        var primaryEmail = user.Emails
            .FirstOrDefault(e => e.IsPrimary)?.Address
            ?? normalizedEmail;

        var tokenData = new UserTokenData(
            UserId: user.Id,
            Email: primaryEmail,
            Roles: roles,
            AdditionalClaims: additionalClaims);

        var accessToken = jwtTokenService.GenerateAccessToken(tokenData);

        return Result<LoginResult>.Success(new LoginResult(user.Id, accessToken));
    }
}
