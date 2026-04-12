namespace YallaJo.Web.Areas.Auth.Features.Sessions.Requests;

/// <summary>Used when the user submits a "Revoke" form for a specific session.</summary>
public sealed class RevokeSessionRequest
{
    public Guid SessionId { get; init; }
}
