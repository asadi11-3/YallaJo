namespace Auth.Presentation.Endpoints.Invitation.Models;

public sealed record AcceptInviteResponse(Guid UserId, string Message);
