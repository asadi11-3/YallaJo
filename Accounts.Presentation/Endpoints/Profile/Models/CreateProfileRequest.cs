namespace Accounts.Presentation.Endpoints.Profile.Models;

public sealed record CreateProfileRequest(
    Guid UserId,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? AvatarUrl);
