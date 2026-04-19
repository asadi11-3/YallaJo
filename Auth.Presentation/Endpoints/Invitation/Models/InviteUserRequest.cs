namespace Auth.Presentation.Endpoints.Invitation.Models;

public sealed record InviteUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? AvatarUrl);
