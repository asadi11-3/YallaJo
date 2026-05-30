namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Lifecycle.Requests;

/// <summary>
/// Phase 5B — wire DTO posted by the Web admin UI to the Phase 3C
/// admin-reassignment endpoint
/// (<c>POST /api/v1/auth/admin/users/{id}/reassign</c>).
/// <para>
/// The actor is resolved server-side from the bearer token; only the
/// new email and an optional reason are carried over the wire.
/// </para>
/// </summary>
public sealed class AdminReassignAccountRequest
{
    public string  NewEmail { get; init; } = string.Empty;
    public string? Reason   { get; init; }
}
