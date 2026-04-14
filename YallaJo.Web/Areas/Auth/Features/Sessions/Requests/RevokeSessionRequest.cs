namespace YallaJo.Web.Areas.Auth.Features.Sessions.Requests;


public sealed class RevokeSessionRequest
{
    public Guid SessionId { get; init; }
}
