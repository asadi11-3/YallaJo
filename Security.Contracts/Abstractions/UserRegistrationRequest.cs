namespace Security.Contracts.Abstractions;

public sealed record UserRegistrationRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);
