namespace Security.Contracts.Abstractions;

public sealed record InvitedUserRegistrationRequest(
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyList<Guid> InitialRoleIds);
