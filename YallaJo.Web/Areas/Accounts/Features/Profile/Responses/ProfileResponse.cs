namespace YallaJo.Web.Areas.Accounts.Features.Profile.Responses;

public sealed class ProfileResponse
{
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName  { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? AvatarUrl   { get; init; }
    public string? PhoneNumber { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? Country { get; init; }
    public string? City { get; init; }
    public string? AddressLine { get; init; }
    public string Email { get; init; } = string.Empty;
}
