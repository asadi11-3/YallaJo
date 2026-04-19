using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Contracts.Abstractions;

public interface IUserRegistrationService
{
    Task<Result<Guid>> RegisterAsync(
        UserRegistrationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record UserRegistrationRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);
