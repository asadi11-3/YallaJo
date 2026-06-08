namespace YallaJo.Web.Features.AdminNav;

/// <summary>
/// Web boundary contract for GET /api/v1/security/me — the DB-backed roles &amp;
/// permissions snapshot that drives admin navigation visibility (plan §9 line 13).
/// Mirrors the backend SecurityMeDto shape.
/// </summary>
public sealed class SecurityMeResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = [];
    public IReadOnlyList<string> Permissions { get; init; } = [];
}
