namespace Auth.Presentation.Endpoints.Invitation.Models;

public sealed record AcceptInviteRequest(
    string Email,
    string Token,
    string Password,
    string ConfirmPassword);
