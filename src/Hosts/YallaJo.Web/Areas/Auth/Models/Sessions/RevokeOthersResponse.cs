namespace YallaJo.Web.Areas.Auth.Models.Sessions;

/// <summary>Response of POST /api/v1/auth/sessions/revoke-others.</summary>
public sealed record RevokeOthersResponse(int RevokedCount);
