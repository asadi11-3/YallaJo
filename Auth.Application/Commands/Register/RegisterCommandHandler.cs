using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.Register;

/// <summary>
/// Auth-side orchestration for new-account registration.
/// <para>
/// This handler does NOT write to any Security aggregate directly — it only
/// delegates to <see cref="IUserRegistrationService"/> exposed by
/// <c>Security.Contracts</c>. Identity ownership remains in Security.
/// </para>
/// </summary>
public sealed class RegisterCommandHandler(
    IUserRegistrationService userRegistrationService)
    : ICommandHandler<RegisterCommand, RegisterResult>
{
    public async Task<Result<RegisterResult>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var result = await userRegistrationService.RegisterAsync(
            new UserRegistrationRequest(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password),
            cancellationToken);

        return result.Map(userId => new RegisterResult(userId));
    }
}
