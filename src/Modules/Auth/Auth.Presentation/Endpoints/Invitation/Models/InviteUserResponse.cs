namespace Auth.Presentation.Endpoints.Invitation.Models;

public sealed record InviteUserResponse(Guid UserId, Guid ProfileId, string Message);
